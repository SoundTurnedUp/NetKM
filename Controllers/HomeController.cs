using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NetKM.Models;
using NetKM.Services;
using System.Security.Claims;

namespace NetKM.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly IPostService _postService;
        private readonly ICommentService _commentService;
        private readonly IUserService _userService;
        private readonly IFileUploadService _fileUploadService;
        private readonly UserManager<User> _userManager;

        public HomeController(
            IPostService postService,
            ICommentService commentService,
            IUserService userService,
            IFileUploadService fileUploadService,
            UserManager<User> userManager)
        {
            _postService = postService;
            _commentService = commentService;
            _userService = userService;
            _fileUploadService = fileUploadService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(int page = 1)
        {
            // For testing
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }

            var posts = await _postService.GetAllPostsAsync(page, 20);
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            ViewBag.UserId = userId;
            ViewBag.CurrentPage = page;

            return View(posts);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePost(string content, IFormFile media)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            string mediaUrl = null;

            try
            {
                if (media != null && media.Length > 0)
                {
                    mediaUrl = await _fileUploadService.UploadFileAsync(media, "posts");
                }

                await _postService.CreatePostAsync(userId, content, mediaUrl);
                TempData["Success"] = "Post created successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePost(Guid postId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _postService.DeletePostAsync(postId, userId);

            if (result)
                TempData["Success"] = "Post deleted successfully!";
            else
                TempData["Error"] = "Failed to delete post.";

            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> GetPost(Guid postId)
        {
            var post = await _postService.GetPostByIdAsync(postId);
            if (post == null)
                return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var hasLiked = await _postService.HasUserLikedPostAsync(postId, userId);

            return Json(new
            {
                postId = post.PostId,
                authorName = $"{post.Author.FirstName} {post.Author.LastName}",
                authorAvatar = post.Author.ProfilePictureURL,
                content = post.Content,
                mediaUrl = post.MediaURL,
                likeCount = post.Likes.Count,
                commentCount = post.Comments.Count,
                hasLiked = hasLiked
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetComments(Guid postId)
        {
            var comments = await _commentService.GetCommentsByPostAsync(postId, 0, 100);
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userService.GetUserByIdAsync(userId);

            var commentData = comments.Select(c => new
            {
                commentId = c.CommentId,
                authorName = $"{c.Author.FirstName} {c.Author.LastName}",
                authorAvatar = c.Author.ProfilePictureURL,
                content = c.Content,
                timeAgo = GetTimeAgo(c.CreatedAt),
                canDelete = c.AuthorId == userId || user.Role == "Admin" || user.Role == "Teacher"
            });

            return Json(commentData);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteComment(Guid commentId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _commentService.DeleteCommentAsync(commentId, userId);

            return Json(new { success = result });
        }

        private string GetTimeAgo(DateTime dateTime)
        {
            var timeSpan = DateTime.UtcNow - dateTime;

            if (timeSpan.TotalSeconds < 60)
                return "just now";
            if (timeSpan.TotalMinutes < 60)
                return $"{(int)timeSpan.TotalMinutes}m ago";
            if (timeSpan.TotalHours < 24)
                return $"{(int)timeSpan.TotalHours}h ago";
            if (timeSpan.TotalDays < 7)
                return $"{(int)timeSpan.TotalDays}d ago";
            if (timeSpan.TotalDays < 30)
                return $"{(int)(timeSpan.TotalDays / 7)}w ago";

            return dateTime.ToString("MMM dd, yyyy");
        }

        [HttpPost]
        public async Task<IActionResult> LikePost(Guid postId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var hasLiked = await _postService.HasUserLikedPostAsync(postId, userId);

            if (hasLiked)
                await _postService.UnlikePostAsync(postId, userId);
            else
                await _postService.LikePostAsync(postId, userId);

            var likeCount = await _postService.GetLikeCountAsync(postId);
            return Json(new { success = true, likeCount, hasLiked = !hasLiked });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(Guid postId, string content)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            try
            {
                var comment = await _commentService.CreateCommentAsync(postId, userId, content);
                return Json(new
                {
                    success = true,
                    commentId = comment.CommentId,
                    authorName = $"{comment.Author.FirstName} {comment.Author.LastName}",
                    content = comment.Content,
                    createdAt = comment.CreatedAt.ToString("MMM dd, yyyy")
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> Profile(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null)
                return NotFound();

            var posts = await _postService.GetPostsByUserAsync(userId, 10);
            var friends = await _userService.GetFriendsAsync(userId);
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var areFriends = currentUserId != userId && await _userService.AreFriendsAsync(currentUserId, userId);

            ViewBag.Posts = posts;
            ViewBag.Friends = friends;
            ViewBag.AreFriends = areFriends;
            ViewBag.IsOwnProfile = currentUserId == userId;

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(string bio, IFormFile profilePicture)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            string profilePictureUrl = null;

            try
            {
                if (profilePicture != null && profilePicture.Length > 0)
                {
                    profilePictureUrl = await _fileUploadService.UploadFileAsync(profilePicture, "profiles");
                }

                await _userService.UpdateProfileAsync(userId, bio, profilePictureUrl);
                TempData["Success"] = "Profile updated successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction("Profile", new { userId });
        }

        [HttpPost]
        public async Task<IActionResult> SendFriendRequest(string receiverId)
        {
            var senderId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _userService.SendFriendRequestAsync(senderId, receiverId);

            return Json(new { success = result });
        }

        [HttpPost]
        public async Task<IActionResult> AcceptFriendRequest(Guid requestId)
        {
            var result = await _userService.AcceptFriendRequestAsync(requestId);
            return Json(new { success = result });
        }

        [HttpPost]
        public async Task<IActionResult> DeclineFriendRequest(Guid requestId)
        {
            var result = await _userService.DeclineFriendRequestAsync(requestId);
            return Json(new { success = result });
        }

        public async Task<IActionResult> Friends()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var friends = await _userService.GetFriendsAsync(userId);
            var pendingRequests = await _userService.GetPendingFriendRequestsAsync(userId);

            ViewBag.PendingRequests = pendingRequests;
            return View(friends);
        }
    }
}
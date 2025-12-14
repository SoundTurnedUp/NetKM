using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetKM.Data;
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
        private readonly IMessageService _messageService;
        private readonly UserManager<User> _userManager;
        private readonly ApplicationDbContext _context;

        public HomeController(
            IPostService postService,
            ICommentService commentService,
            IUserService userService,
            IFileUploadService fileUploadService,
            IMessageService messageService,
            UserManager<User> userManager,
            ApplicationDbContext context)
        {
            _postService = postService;
            _commentService = commentService;
            _userService = userService;
            _fileUploadService = fileUploadService;
            _messageService = messageService;
            _userManager = userManager;
            _context = context;
        }

        // GET: /Home/Index - Main Feed
        public async Task<IActionResult> Index(int page = 1)
        {
            // For testing - if views aren't loading
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

        // POST: /Home/CreatePost
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

        // POST: /Home/DeletePost
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

        // GET: /Home/GetPost (for modal)
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
                authorId = post.AuthorId,
                authorName = $"{post.Author.FirstName} {post.Author.LastName}",
                authorAvatar = post.Author.ProfilePictureURL,
                content = post.Content,
                mediaUrl = post.MediaURL,
                likeCount = post.Likes.Count,
                commentCount = post.Comments.Count,
                hasLiked = hasLiked
            });
        }

        // GET: /Home/GetComments (for modal)
        [HttpGet]
        public async Task<IActionResult> GetComments(Guid postId)
        {
            var comments = await _commentService.GetCommentsByPostAsync(postId, 0, 100);
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userService.GetUserByIdAsync(userId);

            var commentData = comments.Select(c => new
            {
                commentId = c.CommentId,
                authorId = c.AuthorId,
                authorName = $"{c.Author.FirstName} {c.Author.LastName}",
                authorAvatar = c.Author.ProfilePictureURL,
                content = c.Content,
                timeAgo = GetTimeAgo(c.CreatedAt),
                canDelete = c.AuthorId == userId || user.Role == "Admin" || user.Role == "Teacher"
            });

            return Json(commentData);
        }

        // POST: /Home/DeleteComment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteComment(Guid commentId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _commentService.DeleteCommentAsync(commentId, userId);

            return Json(new { success = result });
        }

        // Helper method to get time ago string
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

        // POST: /Home/LikePost
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

        // POST: /Home/AddComment
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

        // GET: /Home/Profile/{userId}
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

        // POST: /Home/UpdateProfile
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

        // POST: /Home/SendFriendRequest
        [HttpPost]
        public async Task<IActionResult> SendFriendRequest(string receiverId)
        {
            var senderId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _userService.SendFriendRequestAsync(senderId, receiverId);

            return Json(new { success = result });
        }

        // POST: /Home/AcceptFriendRequest
        [HttpPost]
        public async Task<IActionResult> AcceptFriendRequest(Guid requestId)
        {
            var result = await _userService.AcceptFriendRequestAsync(requestId);
            return Json(new { success = result });
        }

        // POST: /Home/DeclineFriendRequest
        [HttpPost]
        public async Task<IActionResult> DeclineFriendRequest(Guid requestId)
        {
            var result = await _userService.DeclineFriendRequestAsync(requestId);
            return Json(new { success = result });
        }

        // GET: /Home/Friends
        public async Task<IActionResult> Friends()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var friends = await _userService.GetFriendsAsync(userId);
            var pendingRequests = await _userService.GetPendingFriendRequestsAsync(userId);

            ViewBag.PendingRequests = pendingRequests;
            return View(friends);
        }

        // GET: /Home/Messages
        public async Task<IActionResult> Messages(string userId = null)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var friends = await _userService.GetFriendsAsync(currentUserId);
            var currentUser = await _userService.GetUserByIdAsync(currentUserId);

            var viewModel = new MessagesViewModel
            {
                CurrentUserId = currentUserId,
                CurrentUserAvatar = currentUser.ProfilePictureURL
            };

            // Get conversations with last message info
            foreach (var friend in friends)
            {
                var lastMessage = await _messageService.GetLastMessageAsync(currentUserId, friend.Id);
                viewModel.Conversations.Add(new ConversationViewModel
                {
                    UserId = friend.Id,
                    UserName = $"{friend.FirstName} {friend.LastName}",
                    Avatar = friend.ProfilePictureURL,
                    LastMessage = lastMessage?.Content,
                    LastMessageTime = lastMessage?.SentAt,
                    UnreadCount = 0
                });
            }

            viewModel.Conversations = viewModel.Conversations.OrderByDescending(c => c.LastMessageTime).ToList();

            // If a specific user is selected, load their conversation
            if (!string.IsNullOrEmpty(userId))
            {
                var activeUser = await _userService.GetUserByIdAsync(userId);
                var messages = await _messageService.GetConversationAsync(currentUserId, userId, 100);

                viewModel.ActiveUserId = userId;
                viewModel.ActiveUserName = $"{activeUser.FirstName} {activeUser.LastName}";
                viewModel.ActiveUserAvatar = activeUser.ProfilePictureURL;
                viewModel.Messages = messages;
            }

            return View(viewModel);
        }

        // POST: /Home/SendMessage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMessage(string receiverId, string content)
        {
            var senderId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            try
            {
                // Check if they're friends
                var areFriends = await _userService.AreFriendsAsync(senderId, receiverId);
                if (!areFriends)
                {
                    return Json(new { success = false, message = "You can only message friends" });
                }

                var message = await _messageService.SendMessageAsync(senderId, receiverId, content, null);

                return Json(new
                {
                    success = true,
                    message = new
                    {
                        content = message.Content,
                        sentAt = message.SentAt,
                        senderId = message.SenderId
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: /Home/GetNewMessages
        [HttpGet]
        public async Task<IActionResult> GetNewMessages(string userId, DateTime since)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var messages = await _messageService.GetConversationAsync(currentUserId, userId, 50);

            var newMessages = messages
                .Where(m => m.SentAt > since && m.SenderId == userId)
                .Select(m => new
                {
                    content = m.Content,
                    sentAt = m.SentAt,
                    senderId = m.SenderId
                })
                .ToList();

            return Json(new { messages = newMessages });
        }

        // POST: /Home/ReportPost
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReportPost(Guid postId, string reason)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            try
            {
                // Check if post exists
                var post = await _postService.GetPostByIdAsync(postId);
                if (post == null)
                {
                    return Json(new { success = false, message = "Post not found" });
                }

                // Check if user is trying to report their own post
                if (post.AuthorId == userId)
                {
                    return Json(new { success = false, message = "You cannot report your own post" });
                }

                // Create report
                var report = new Report
                {
                    ReporterId = userId,
                    ContentId = postId,
                    ContentType = "Post",
                    Reason = reason,
                    Status = "Pending",
                    CreatedAt = DateTime.UtcNow
                };

                await _context.Reports.AddAsync(report);
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: /Home/ReportComment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReportComment(Guid commentId, string reason)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            try
            {
                // Check if comment exists
                var comment = await _context.Comments
                    .Include(c => c.Author)
                    .FirstOrDefaultAsync(c => c.CommentId == commentId);

                if (comment == null)
                {
                    return Json(new { success = false, message = "Comment not found" });
                }

                // Check if user is trying to report their own comment
                if (comment.AuthorId == userId)
                {
                    return Json(new { success = false, message = "You cannot report your own comment" });
                }

                // Create report
                var report = new Report
                {
                    ReporterId = userId,
                    ContentId = commentId,
                    ContentType = "Comment",
                    Reason = reason,
                    Status = "Pending",
                    CreatedAt = DateTime.UtcNow
                };

                await _context.Reports.AddAsync(report);
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
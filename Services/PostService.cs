using Microsoft.EntityFrameworkCore;
using NetKM.Data;
using NetKM.Models;

namespace NetKM.Services
{
    public interface IPostService
    {
        Task<Post> CreatePostAsync(string authorId, string content, string mediaUrl);
        Task<List<Post>> GetAllPostsAsync(int page = 1, int pageSize = 20);
        Task<List<Post>> GetPostsByUserAsync(string userId, int count = 10);
        Task<Post> GetPostByIdAsync(Guid postId);
        Task<bool> DeletePostAsync(Guid postId, string userId);
        Task<bool> LikePostAsync(Guid postId, string userId);
        Task<bool> UnlikePostAsync(Guid postId, string userId);
        Task<bool> HasUserLikedPostAsync(Guid postId, string userId);
        Task<int> GetLikeCountAsync(Guid postId);
    }

    public class PostService : IPostService
    {
        private readonly ApplicationDbContext _context;

        public PostService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Post> CreatePostAsync(string authorId, string content, string mediaUrl)
        {
            // Validate content
            if (string.IsNullOrWhiteSpace(content) && string.IsNullOrWhiteSpace(mediaUrl))
            {
                throw new ArgumentException("Post must have either content or media");
            }

            if (!string.IsNullOrWhiteSpace(content) && content.Length > 2000)
            {
                throw new ArgumentException("Post content exceeds maximum length of 2000 characters");
            }

            var post = new Post
            {
                AuthorId = authorId,
                Content = content ?? string.Empty,
                MediaURL = mediaUrl,
                CreatedAt = DateTime.UtcNow
            };

            _context.Posts.Add(post);
            await _context.SaveChangesAsync();

            // Return post with author included
            return await _context.Posts
                .Include(p => p.Author)
                .FirstOrDefaultAsync(p => p.PostId == post.PostId);
        }

        public async Task<List<Post>> GetAllPostsAsync(int page = 1, int pageSize = 20)
        {
            return await _context.Posts
                .Include(p => p.Author)
                .Include(p => p.Likes)
                .Include(p => p.Comments)
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<List<Post>> GetPostsByUserAsync(string userId, int count = 10)
        {
            return await _context.Posts
                .Where(p => p.AuthorId == userId)
                .Include(p => p.Author)
                .Include(p => p.Likes)
                .Include(p => p.Comments)
                .OrderByDescending(p => p.CreatedAt)
                .Take(count)
                .ToListAsync();
        }

        public async Task<Post> GetPostByIdAsync(Guid postId)
        {
            return await _context.Posts
                .Include(p => p.Author)
                .Include(p => p.Likes)
                .Include(p => p.Comments)
                    .ThenInclude(c => c.Author)
                .FirstOrDefaultAsync(p => p.PostId == postId);
        }

        public async Task<bool> DeletePostAsync(Guid postId, string userId)
        {
            var post = await _context.Posts.FindAsync(postId);
            if (post == null) return false;

            // Check if user is the author or admin
            var user = await _context.Users.FindAsync(userId);
            if (post.AuthorId != userId && user?.Role != "Admin" && user?.Role != "Teacher")
            {
                return false;
            }

            _context.Posts.Remove(post);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> LikePostAsync(Guid postId, string userId)
        {
            // Check if already liked
            var existingLike = await _context.Likes
                .FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == userId);

            if (existingLike != null) return false;

            var like = new Like
            {
                PostId = postId,
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Likes.Add(like);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UnlikePostAsync(Guid postId, string userId)
        {
            var like = await _context.Likes
                .FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == userId);

            if (like == null) return false;

            _context.Likes.Remove(like);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> HasUserLikedPostAsync(Guid postId, string userId)
        {
            return await _context.Likes
                .AnyAsync(l => l.PostId == postId && l.UserId == userId);
        }

        public async Task<int> GetLikeCountAsync(Guid postId)
        {
            return await _context.Likes
                .CountAsync(l => l.PostId == postId);
        }
    }
}

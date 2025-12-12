using Microsoft.EntityFrameworkCore;
using NetKM.Data;
using NetKM.Models;

namespace NetKM.Services
{
    public interface ICommentService
    {
        Task<Comment> CreateCommentAsync(Guid postId, string authorId, string content);
        Task<List<Comment>> GetCommentsByPostAsync(Guid postId, int skip = 0, int take = 10);
        Task<bool> DeleteCommentAsync(Guid commentId, string userId);
    }

    public class CommentService : ICommentService
    {
        private readonly ApplicationDbContext _context;

        public CommentService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Comment> CreateCommentAsync(Guid postId, string authorId, string content)
        {
            if (string.IsNullOrWhiteSpace(content) || content.Length > 200)
            {
                throw new ArgumentException("Comment must be between 1 and 200 characters");
            }

            var comment = new Comment
            {
                PostId = postId,
                AuthorId = authorId,
                Content = content,
                CreatedAt = DateTime.UtcNow
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            return await _context.Comments
                .Include(c => c.Author)
                .FirstOrDefaultAsync(c => c.CommentId == comment.CommentId);
        }

        public async Task<List<Comment>> GetCommentsByPostAsync(Guid postId, int skip = 0, int take = 10)
        {
            return await _context.Comments
                .Where(c => c.PostId == postId)
                .Include(c => c.Author)
                .OrderByDescending(c => c.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        public async Task<bool> DeleteCommentAsync(Guid commentId, string userId)
        {
            var comment = await _context.Comments.FindAsync(commentId);
            if (comment == null) return false;

            var user = await _context.Users.FindAsync(userId);
            if (comment.AuthorId != userId && user?.Role != "Admin" && user?.Role != "Teacher")
            {
                return false;
            }

            _context.Comments.Remove(comment);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}

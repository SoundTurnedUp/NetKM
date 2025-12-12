using Microsoft.EntityFrameworkCore;
using NetKM.Models;
using NetKM.Data;

namespace NetKM.Services
{
    public interface IMessageService
    {
        Task<Message> SendMessageAsync(string senderId, string receiverId, string content, string mediaUrl);
        Task<List<Message>> GetConversationAsync(string userId1, string userId2, int count = 50);
        Task<List<Message>> GetUnreadMessagesAsync(string userId);
        Task<bool> MarkAsReadAsync(Guid messageId);
    }

    public class MessageService : IMessageService
    {
        private readonly ApplicationDbContext _context;

        public MessageService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Message> SendMessageAsync(string senderId, string receiverId, string content, string mediaUrl)
        {
            if (string.IsNullOrWhiteSpace(content) && string.IsNullOrWhiteSpace(mediaUrl))
            {
                throw new ArgumentException("Message must have content or media");
            }

            var message = new Message
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                Content = content ?? string.Empty,
                MediaURL = mediaUrl,
                SentAt = DateTime.UtcNow,
                IsRead = false
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            return await _context.Messages
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .FirstOrDefaultAsync(m => m.MessageId == message.MessageId);
        }

        public async Task<List<Message>> GetConversationAsync(string userId1, string userId2, int count = 50)
        {
            return await _context.Messages
                .Where(m =>
                    (m.SenderId == userId1 && m.ReceiverId == userId2) ||
                    (m.SenderId == userId2 && m.ReceiverId == userId1))
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .OrderByDescending(m => m.SentAt)
                .Take(count)
                .ToListAsync();
        }

        public async Task<List<Message>> GetUnreadMessagesAsync(string userId)
        {
            return await _context.Messages
                .Where(m => m.ReceiverId == userId && !m.IsRead)
                .Include(m => m.Sender)
                .OrderByDescending(m => m.SentAt)
                .ToListAsync();
        }

        public async Task<bool> MarkAsReadAsync(Guid messageId)
        {
            var message = await _context.Messages.FindAsync(messageId);
            if (message == null) return false;

            message.IsRead = true;
            _context.Messages.Update(message);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}

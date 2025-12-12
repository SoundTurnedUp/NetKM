using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NetKM.Data;
using NetKM.Models;
using System.Security.Cryptography;
using System.Text;

namespace NetKM.Services
{
    public interface IUserService
    {
        Task<User> GetUserByIdAsync(string userId);
        Task<User> GetUserByEmailAsync(string email);
        Task<bool> UpdateProfileAsync(string userId, string bio, string profilePictureUrl);
        Task<bool> UpdateLastLoginAsync(string userId);
        Task<List<User>> GetFriendsAsync(string userId);
        Task<bool> SendFriendRequestAsync(string senderId, string receiverId);
        Task<bool> AcceptFriendRequestAsync(Guid requestId);
        Task<bool> DeclineFriendRequestAsync(Guid requestId);
        Task<List<FriendRequest>> GetPendingFriendRequestsAsync(string userId);
        Task<bool> AreFriendsAsync(string userId1, string userId2);
        string GenerateProfilePicture(string firstName, string lastName);
    }

    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public UserService(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<User> GetUserByIdAsync(string userId)
        {
            return await _context.Users
                .Include(u => u.Posts)
                .Include(u => u.UserGroups)
                .FirstOrDefaultAsync(u => u.Id == userId);
        }

        public async Task<User> GetUserByEmailAsync(string email)
        {
            return await _userManager.FindByEmailAsync(email);
        }

        public async Task<bool> UpdateProfileAsync(string userId, string bio, string profilePictureUrl)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            user.Bio = bio;
            if (!string.IsNullOrEmpty(profilePictureUrl))
            {
                user.ProfilePictureURL = profilePictureUrl;
            }

            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateLastLoginAsync(string userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            user.LastLogin = DateTime.UtcNow;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<User>> GetFriendsAsync(string userId)
        {
            var friends = new List<User>();

            // Get accepted friend requests where user is sender
            var sentRequests = await _context.FriendRequests
                .Where(fr => fr.SenderId == userId && fr.Status == "Accepted")
                .Include(fr => fr.Receiver)
                .Select(fr => fr.Receiver)
                .ToListAsync();

            // Get accepted friend requests where user is receiver
            var receivedRequests = await _context.FriendRequests
                .Where(fr => fr.ReceiverId == userId && fr.Status == "Accepted")
                .Include(fr => fr.Sender)
                .Select(fr => fr.Sender)
                .ToListAsync();

            friends.AddRange(sentRequests);
            friends.AddRange(receivedRequests);

            return friends.Take(20).ToList();
        }

        public async Task<bool> SendFriendRequestAsync(string senderId, string receiverId)
        {
            // Check if friend request already exists
            var existingRequest = await _context.FriendRequests
                .FirstOrDefaultAsync(fr =>
                    (fr.SenderId == senderId && fr.ReceiverId == receiverId) ||
                    (fr.SenderId == receiverId && fr.ReceiverId == senderId));

            if (existingRequest != null) return false;

            // Prevent self friend requests
            if (senderId == receiverId) return false;

            var friendRequest = new FriendRequest
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            _context.FriendRequests.Add(friendRequest);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> AcceptFriendRequestAsync(Guid requestId)
        {
            var request = await _context.FriendRequests.FindAsync(requestId);
            if (request == null || request.Status != "Pending") return false;

            request.Status = "Accepted";
            request.RespondedAt = DateTime.UtcNow;

            _context.FriendRequests.Update(request);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeclineFriendRequestAsync(Guid requestId)
        {
            var request = await _context.FriendRequests.FindAsync(requestId);
            if (request == null || request.Status != "Pending") return false;

            request.Status = "Declined";
            request.RespondedAt = DateTime.UtcNow;

            _context.FriendRequests.Update(request);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<FriendRequest>> GetPendingFriendRequestsAsync(string userId)
        {
            return await _context.FriendRequests
                .Where(fr => fr.ReceiverId == userId && fr.Status == "Pending")
                .Include(fr => fr.Sender)
                .OrderByDescending(fr => fr.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> AreFriendsAsync(string userId1, string userId2)
        {
            return await _context.FriendRequests
                .AnyAsync(fr =>
                    fr.Status == "Accepted" &&
                    ((fr.SenderId == userId1 && fr.ReceiverId == userId2) ||
                     (fr.SenderId == userId2 && fr.ReceiverId == userId1)));
        }

        public string GenerateProfilePicture(string firstName, string lastName)
        {
            string initials = $"{firstName[0]}{lastName[0]}".ToUpper();

            string nameHash = $"{firstName}{lastName}";
            using (var md5 = MD5.Create())
            {
                byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(nameHash));
                int colourValue = Math.Abs(BitConverter.ToInt32(hash, 0));

                int hue = colourValue % 360;
                string color = $"hsl({hue}, 70%, 50%)";

                string svg = $@"<svg xmlns='http://www.w3.org/2000/svg' width='100' height='100'>
                    <rect width='100' height='100' fill='{color}'/>
                    <text x='50' y='50' font-size='40' fill='white' text-anchor='middle' dy='.3em' font-family='Arial'>{initials}</text>
                </svg>";

                byte[] svgBytes = Encoding.UTF8.GetBytes(svg);
                return $"data:image/svg+xml;base64,{Convert.ToBase64String(svgBytes)}";
            }
        }
    }
}

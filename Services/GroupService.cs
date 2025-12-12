using Microsoft.EntityFrameworkCore;
using NetKM.Data;
using NetKM.Models;

namespace NetKM.Services
{
    public interface IGroupService
    {
        Task<Group> CreateGroupAsync(string name, string code, string description, string ownerId);
        Task<bool> JoinGroupAsync(Guid groupId, string userId);
        Task<bool> LeaveGroupAsync(Guid groupId, string userId);
        Task<Group> GetGroupByCodeAsync(string code);
        Task<List<Group>> GetUserGroupsAsync(string userId);
        Task<List<User>> GetGroupMembersAsync(Guid groupId);
    }

    public class GroupService : IGroupService
    {
        private readonly ApplicationDbContext _context;

        public GroupService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Group> CreateGroupAsync(string name, string code, string description, string ownerId)
        {
            var group = new Group
            {
                Name = name,
                Code = code,
                Description = description,
                CreatedAt = DateTime.UtcNow
            };

            _context.Groups.Add(group);
            await _context.SaveChangesAsync();

            // Add creator as owner
            var userGroup = new UserGroup
            {
                GroupId = group.GroupId,
                UserId = ownerId,
                Role = "Owner",
                JoinedAt = DateTime.UtcNow
            };

            _context.UserGroups.Add(userGroup);
            await _context.SaveChangesAsync();

            return group;
        }

        public async Task<bool> JoinGroupAsync(Guid groupId, string userId)
        {
            var existing = await _context.UserGroups
                .FirstOrDefaultAsync(ug => ug.GroupId == groupId && ug.UserId == userId);

            if (existing != null) return false;

            var userGroup = new UserGroup
            {
                GroupId = groupId,
                UserId = userId,
                Role = "Member",
                JoinedAt = DateTime.UtcNow
            };

            _context.UserGroups.Add(userGroup);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> LeaveGroupAsync(Guid groupId, string userId)
        {
            var userGroup = await _context.UserGroups
                .FirstOrDefaultAsync(ug => ug.GroupId == groupId && ug.UserId == userId);

            if (userGroup == null) return false;

            _context.UserGroups.Remove(userGroup);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<Group> GetGroupByCodeAsync(string code)
        {
            return await _context.Groups
                .Include(g => g.UserGroups)
                .ThenInclude(ug => ug.User)
                .FirstOrDefaultAsync(g => g.Code == code);
        }

        public async Task<List<Group>> GetUserGroupsAsync(string userId)
        {
            return await _context.UserGroups
                .Where(ug => ug.UserId == userId)
                .Include(ug => ug.Group)
                .Select(ug => ug.Group)
                .ToListAsync();
        }

        public async Task<List<User>> GetGroupMembersAsync(Guid groupId)
        {
            return await _context.UserGroups
                .Where(ug => ug.GroupId == groupId)
                .Include(ug => ug.User)
                .Select(ug => ug.User)
                .ToListAsync();
        }
    }
}

using System.ComponentModel.DataAnnotations;

namespace NetKM.Models
{
    public class UserGroup
    {
        [Required]
        public Guid GroupId { get; set; }

        [Required]
        public string UserId { get; set; }

        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

        [Required]
        [MaxLength(20)]
        public string Role { get; set; } = "Member";

        // Navigation
        public virtual Group Group { get; set; }
        public virtual User User { get; set; }
    }
}

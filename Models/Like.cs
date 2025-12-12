using System.ComponentModel.DataAnnotations;

namespace NetKM.Models
{
    public class Like
    {
        [Key]
        public Guid LikeId { get; set; } = Guid.NewGuid();

        [Required]
        public Guid PostId { get; set; }

        [Required]
        public string UserId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation 
        public virtual Post Post { get; set; }
        public virtual User User { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;

namespace NetKM.Models
{
    public class Comment
    {
        [Key]
        public Guid CommentId { get; set; } = Guid.NewGuid();

        [Required]
        public Guid PostId { get; set; }

        [Required]
        public string AuthorId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Content { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation 
        public virtual Post Post { get; set; }
        public virtual User Author { get; set; }
    }
}

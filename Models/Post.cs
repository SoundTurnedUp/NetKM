using System.ComponentModel.DataAnnotations;

namespace NetKM.Models
{
    public class Post
    {
        [Key]
        public Guid PostId { get; set; } = Guid.NewGuid();

        [Required]
        public string AuthorId { get; set; }

        [Required]
        [MaxLength(2000)]
        public string Content { get; set; }

        [MaxLength(512)]
        public string? MediaURL { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool Edited { get; set; } = false;

        // Navigation 
        public virtual User Author { get; set; }
        public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public virtual ICollection<Like> Likes { get; set; } = new List<Like>();
    }
}

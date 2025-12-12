using System.ComponentModel.DataAnnotations;

namespace NetKM.Models
{
    public class Message
    {
        [Key]
        public Guid MessageId { get; set; } = Guid.NewGuid();

        [Required]
        public string SenderId { get; set; }

        [Required]
        public string ReceiverId { get; set; }

        [Required]
        [MaxLength(500)]
        public string Content { get; set; }

        [MaxLength(512)]
        public string? MediaURL { get; set; }

        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        public bool IsRead { get; set; } = false;

        // Navigation 
        public virtual User Sender { get; set; }
        public virtual User Receiver { get; set; }
    }
}

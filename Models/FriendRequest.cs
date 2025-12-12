using System.ComponentModel.DataAnnotations;

namespace NetKM.Models
{
    public class FriendRequest
    {
        [Key]
        public Guid RequestId { get; set; } = Guid.NewGuid();

        [Required]
        public string SenderId { get; set; }

        [Required]
        public string ReceiverId { get; set; }

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Pending";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? RespondedAt { get; set; }

        // Navigation 
        public virtual User Sender { get; set; }
        public virtual User Receiver { get; set; }
    }
}

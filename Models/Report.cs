using System.ComponentModel.DataAnnotations;

namespace NetKM.Models
{
    public class Report
    {
        [Key]
        public Guid ReportId { get; set; } = Guid.NewGuid();

        [Required]
        public string ReporterId { get; set; }

        [Required]
        public Guid ContentId { get; set; }  

        [Required]
        [MaxLength(20)]
        public string ContentType { get; set; } // Can be post or comment

        [MaxLength(500)]
        public string? Reason { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(20)]
        public string Status { get; set; } = "Pending";

        public virtual User Reporter { get; set; }
    }
}

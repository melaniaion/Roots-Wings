using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Roots_Wings.Models
{
    public class TherapySession
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; }

        [Required]
        public DateTime SessionDate { get; set; }

        public string? PrepNotes { get; set; }

        public string? PostNotes { get; set; }

        [MaxLength(150)]
        public string? TherapistName { get; set; }

        [Required]
        [MaxLength(50)]
        public string TherapyType { get; set; } = "Unsure";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public string UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }
    }
}
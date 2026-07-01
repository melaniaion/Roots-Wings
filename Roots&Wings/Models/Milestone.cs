using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Roots_Wings.Models
{
    public class Milestone
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [StringLength(200, MinimumLength = 2, ErrorMessage = "The milestone must be between 2 and 200 characters long.")]
        public string Description { get; set; }

        // Preserves the order milestones were added / arranged in on the goal card.
        public int Order { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Null until checked off. A timestamp rather than a bool, so each
        // milestone completion is a dated, not just a checkbox flip.
        public DateTime? CompletedAt { get; set; }

        [NotMapped]
        public bool IsCompleted => CompletedAt.HasValue;

        [Required]
        public int GoalId { get; set; }

        [ForeignKey("GoalId")]
        public virtual Goal? Goal { get; set; }
    }
}
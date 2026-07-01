using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace Roots_Wings.Models
{
    public class Goal
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [StringLength(150, MinimumLength = 3, ErrorMessage = "The goal name must be between 3 and 150 characters long.")]
        public string Title { get; set; }


        // Required so every goal is grounded in a reason rather than being a bare task.
        [Required(ErrorMessage = "Add a reason this goal matters to you.")]
        [StringLength(500, MinimumLength = 5, ErrorMessage = "The reason must be between 5 and 500 characters long.")]
        public string Motivation { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Null while the goal is active. Set the moment the last milestone is
        // checked off. The goal then moves to the Wins Shelf but is never deleted
        public DateTime? CompletedAt { get; set; }

        [NotMapped]
        public bool IsCompleted => CompletedAt.HasValue;

        [Required]
        public string UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        public virtual ICollection<Milestone> Milestones { get; set; } = new List<Milestone>();
    }
}
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Roots_Wings.Models
{
    public class Tag
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }


        [Required]
        [MaxLength(100)]
        public string Name { get; set; }


        public virtual ICollection<JournalEntry> JournalEntries { get; set; } = new List<JournalEntry>();
    }
}
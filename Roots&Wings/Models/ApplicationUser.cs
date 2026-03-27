using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
namespace Roots_Wings.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required(ErrorMessage = "Name is required!")]
        [StringLength(30, MinimumLength = 3, ErrorMessage = "The Name must be between 3 and 30 characters long.")]
        public string Name { get; set; }
        public string? ProfilePicture { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateLastLogin { get; set; }
    }
}

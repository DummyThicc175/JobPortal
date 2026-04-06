using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace JobPortal.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        public string FullName { get; set; } // Khớp với CandidateProfileViewModel

        public DateTime? BirthDate { get; set; }

        public string? Gender { get; set; }
    }
}
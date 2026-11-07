namespace JobPortal.Models
{
    public class Candidate
    {
        public int Id { get; set; }
        public string? FullName { get; set; }
        public string UserId { get; set; } = "";
        public Microsoft.AspNetCore.Identity.IdentityUser User { get; set; } = null!;
        public string? Phone { get; set; }
        public string? Gender { get; set; }
        public DateTime? Birthday { get; set; }
        public string? AvatarUrl { get; set; }

        public string? Address { get; set; }
        public int? DefaultCVId { get; set; }
        public CV? DefaultCV { get; set; }
    }
}

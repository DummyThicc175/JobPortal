using System.ComponentModel.DataAnnotations;

namespace JobPortal.Models;

public sealed class CandidateProfileViewModel
{
    [Required, StringLength(200)] public string? FullName { get; set; }
    public string? Email { get; set; }
    [Phone, StringLength(30)] public string? Phone { get; set; }
    [StringLength(500)] public string? Address { get; set; }
    [RegularExpression("^(Nam|Nữ|Khác)$")] public string? Gender { get; set; }
    public DateTime? Birthday { get; set; }
    public string? AvatarUrl { get; set; }
    public IFormFile? AvatarFile { get; set; }
    public string? CVUrl { get; set; }
    public IFormFile? CVFile { get; set; }
    public List<Application> AppliedJobs { get; set; } = [];
}

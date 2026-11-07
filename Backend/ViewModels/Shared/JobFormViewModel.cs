using System.ComponentModel.DataAnnotations;

namespace JobPortal.Models;

public sealed class JobFormViewModel
{
    public int Id { get; set; }
    [Required, StringLength(200)] public string Title { get; set; } = "";
    [Required, StringLength(20000)] public string Description { get; set; } = "";
    [Required, StringLength(256)] public string CompanyName { get; set; } = "";
    [StringLength(100)] public string? SalaryRange { get; set; }
    [Required] public int? CategoryId { get; set; }
    [Required] public int? LocationId { get; set; }
}

public sealed class AdminUserViewModel
{
    public string Id { get; set; } = "";
    public string? Email { get; set; }
    public string Role { get; set; } = "Candidate";
    public DateTimeOffset? LockoutEnd { get; set; }
}

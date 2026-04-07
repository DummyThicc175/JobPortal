namespace JobPortal.Models
{
    public class Candidate
    {
        public int Id { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Gender { get; set; }
        public DateTime? Birthday { get; set; }
        public string? MaritalStatus { get; set; }
        public string? Industry { get; set; }
        public string? DesiredLocation { get; set; }
        public string? ExpectedSalary { get; set; }
        public string? AvatarUrl { get; set; }

        public string? Address { get; set; }
        public string? CVUrl { get; set; }
    }
}

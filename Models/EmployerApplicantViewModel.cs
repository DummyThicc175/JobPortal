using System;

namespace JobPortal.Models
{
    public class EmployerApplicantViewModel
    {
        public int Id { get; set; }
        public int JobPostId { get; set; }
        public string JobTitle { get; set; }
        public string CandidateId { get; set; }
        public string CandidateName { get; set; }
        public string CandidateEmail { get; set; }
        public string? CvFileName { get; set; }
        public string? CvUrl { get; set; }
        public DateTime AppliedDate { get; set; }
        public string Status { get; set; }
    }
}

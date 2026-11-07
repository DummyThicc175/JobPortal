using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; 

namespace JobPortal.Models
{
    public class Application
    {
        public int Id { get; set; }

        [Required]
        public int JobPostId { get; set; }
        public virtual JobPost JobPost { get; set; } = null!;

        [Required]
        [MaxLength(450)]
        public string CandidateId { get; set; } = "";

       

        public int? CVId { get; set; }
        public virtual CV? CV { get; set; }

        public DateTime AppliedDate { get; set; } = DateTime.Now;

        [Display(Name = "Trạng thái hồ sơ")]
        public ApplicationStatus Status { get; set; } = ApplicationStatus.Submitted;
        public ICollection<InterviewSession> InterviewSessions { get; set; } = new List<InterviewSession>();
    }

    public enum ApplicationStatus
    {
        Submitted, // Đã nộp
        Reviewing, // Đang xem
        Interview, // Phỏng vấn
        Accepted,  // Đạt
        Rejected   // Không đạt
    }
}

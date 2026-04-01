using System.ComponentModel.DataAnnotations;

namespace JobPortal.Models
{
    public class Application
    {
        public int Id { get; set; }

        [Required]
        public int JobPostId { get; set; }
        public virtual JobPost JobPost { get; set; } // Liên kết đến tin tuyển dụng

        [Required]
        public string CandidateId { get; set; } // ID người ứng tuyển

        public int CVId { get; set; } // CV được chọn để ứng tuyển
        public virtual CV CV { get; set; }

        public DateTime AppliedDate { get; set; } = DateTime.Now;

        [Display(Name = "Trạng thái hồ sơ")]
        public ApplicationStatus Status { get; set; } = ApplicationStatus.Submitted;
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
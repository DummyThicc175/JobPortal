using System.ComponentModel.DataAnnotations;

namespace JobPortal.Models
{
    public class CandidateProfileViewModel
    {
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? Gender { get; set; }
        public DateTime? Birthday { get; set; }

        // Các thông tin hiển thị trên Sidebar (Hình bạn gửi)
        public string? CurrentJobTitle { get; set; } // Ví dụ: Lập trình viên
        public string? AvatarUrl { get; set; }
        public IFormFile? AvatarFile { get; set; }

        // Tiêu chí tìm việc
        public string? Industry { get; set; }
        public string? DesiredLocation { get; set; }
        public string? ExpectedSalary { get; set; }
    }
}
using System.ComponentModel.DataAnnotations;
using JobPortal.Models;

namespace JobPortal.Models // Giữ namespace này để dùng chung toàn hệ thống
{
    public class CandidateProfileViewModel
    {
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? Gender { get; set; }
        public DateTime? Birthday { get; set; }

        // File xử lý tải lên
        public string? AvatarUrl { get; set; }
        public IFormFile? AvatarFile { get; set; }

        public string? CVUrl { get; set; }
        public IFormFile? CVFile { get; set; }

        // Danh sách hiển thị lịch sử ứng tuyển
        public List<Application>? AppliedJobs { get; set; }
    }
}
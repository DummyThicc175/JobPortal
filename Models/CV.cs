using System.ComponentModel.DataAnnotations;

namespace JobPortal.Models
{
    public class CV
    {
        public int Id { get; set; }

        [Required]
        public string CandidateId { get; set; } // ID của người tìm việc (UserId)

        [Display(Name = "Tên file CV")]
        [Required]
        public string FileName { get; set; } // Nên đổi tên từ FilePath thành FileName để dễ quản lý

        [Display(Name = "Tiêu đề CV")]
        public string? Title { get; set; } // Ví dụ: CV Web Developer

        public string? Summary { get; set; } // Tóm tắt bản thân

        [Display(Name = "Ngày tải lên")]
        public DateTime UploadDate { get; set; } = DateTime.Now; // Đổi tên thành UploadDate cho rõ nghĩa
    }
}
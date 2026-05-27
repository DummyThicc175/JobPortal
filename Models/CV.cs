using System.ComponentModel.DataAnnotations;

namespace JobPortal.Models
{
    public class CV
    {
        public int Id { get; set; }

        [Required]
        public string CandidateId { get; set; } // ID của người tìm việc (UserId) [cite: 1]

        [Display(Name = "Tên file CV")]
        public string? FileName { get; set; } // Tên file PDF sau khi xuất (ví dụ: cv_hoang.pdf) [cite: 1]

        [Display(Name = "Tiêu đề CV")]
        public string? Title { get; set; } // Ví dụ: CV Web Developer [cite: 1]

        [Display(Name = "Ngày cập nhật")]
        public DateTime UploadDate { get; set; } = DateTime.Now; // Ngày chỉnh sửa gần nhất [cite: 1]

        // --- CÁC TRƯỜNG DỮ LIỆU ĐIỀN FORM (BỔ SUNG CHO TỰ TẠO CV) ---
        public int? TemplateId { get; set; } // Lưu ID mẫu template ứng viên chọn

        public string? FullName { get; set; }
        public string? JobTitle { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? Website { get; set; }
        public string? Summary { get; set; } // Tóm tắt bản thân [cite: 1]

        // Lưu danh sách dưới dạng chuỗi JSON để tối ưu hóa database, không cần tạo nhiều bảng con cồng kềnh
        public string? ExperiencesJson { get; set; }
        public string? EducationsJson { get; set; }
        public string? SkillsJson { get; set; }

        // --- CÁC TRƯỜNG DỮ LIỆU PHỤC VỤ AI CHẤM ĐIỂM ---
        public int? AiScore { get; set; } // Điểm số từ 1 - 100
        public string? AiFeedback { get; set; } // Lời khuyên chỉnh sửa từ AI
    }
}
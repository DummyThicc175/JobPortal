using System.ComponentModel.DataAnnotations;

namespace JobPortal.Models
{
    public class CV
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(450)]
        public string CandidateId { get; set; } = "";

        [Display(Name = "Tên file CV")]
        [MaxLength(260)]
        public string? FileName { get; set; }

        [Display(Name = "Tiêu đề CV")]
        [MaxLength(200)]
        public string? Title { get; set; }

        [Display(Name = "Ngày cập nhật")]
        public DateTime UploadDate { get; set; } = DateTime.UtcNow;

        // --- CÁC TRƯỜNG DỮ LIỆU ĐIỀN FORM (BỔ SUNG CHO TỰ TẠO CV) ---
        public int? TemplateId { get; set; } // Lưu ID mẫu template ứng viên chọn
        public CVTemplate? Template { get; set; }
        public bool IsSnapshot { get; set; }

        [MaxLength(200)] public string? FullName { get; set; }
        [MaxLength(200)] public string? JobTitle { get; set; }
        [MaxLength(256)] public string? Email { get; set; }
        [MaxLength(30)] public string? Phone { get; set; }
        [MaxLength(500)] public string? Address { get; set; }
        [MaxLength(500)] public string? Website { get; set; }
        [MaxLength(4000)] public string? Summary { get; set; }
        [MaxLength(500)] public string? AvatarPath { get; set; }
        [MaxLength(4000)] public string? Interests { get; set; }
        [MaxLength(4000)] public string? Activities { get; set; }
        [MaxLength(4000)] public string? Certifications { get; set; }
        [MaxLength(7)] public string? ThemeColor { get; set; }
        [MaxLength(7)] public string? HeaderColor { get; set; }
        [MaxLength(7)] public string? BackgroundColor { get; set; }
        [MaxLength(4000)] public string? LayoutJson { get; set; }

        // Các mục chi tiết dùng chung khóa ngoại CVId.
        public List<CvExperience> Experiences { get; set; } = [];
        public List<CvEducation> Educations { get; set; } = [];
        public List<CvSkill> Skills { get; set; } = [];
        public List<CvLanguage> Languages { get; set; } = [];
        public List<CvReference> References { get; set; } = [];

        // --- CÁC TRƯỜNG DỮ LIỆU PHỤC VỤ AI CHẤM ĐIỂM ---
        public int? AiScore { get; set; } // Điểm số từ 1 - 100
        public string? AiFeedback { get; set; } // Lời khuyên chỉnh sửa từ AI
    }
}

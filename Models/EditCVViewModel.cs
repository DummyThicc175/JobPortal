namespace JobPortal.Models
{
    public class EditCVViewModel
    {
        public int Id { get; set; } // Thêm Id cho bảng chính
        public int TemplateId { get; set; }
        public string? TemplatePath { get; set; }

        // Thông tin cơ bản - Thêm dấu ? để tránh lỗi Null
        public string? FullName { get; set; }
        public string? JobTitle { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? Summary { get; set; }

        // Danh sách động
        public List<ExperienceVM> Experiences { get; set; } = new List<ExperienceVM>();
        public List<EducationVM> Educations { get; set; } = new List<EducationVM>();
    }

    public class ExperienceVM
    {
        public int Id { get; set; } // BẮT BUỘC PHẢI CÓ để Migration không lỗi
        public string? Company { get; set; }
        public string? Role { get; set; }
        public string? Duration { get; set; }
        public string? Description { get; set; }

        // Khóa ngoại để biết kinh nghiệm này của CV nào
        public int EditCVViewModelId { get; set; }
    }

    public class EducationVM
    {
        public int Id { get; set; } // BẮT BUỘC PHẢI CÓ
        public string? School { get; set; }
        public string? Major { get; set; }
        public string? Duration { get; set; }

        public int EditCVViewModelId { get; set; }
    }
}
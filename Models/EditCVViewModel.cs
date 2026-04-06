namespace JobPortal.Models
{
    public class EditCVViewModel
    {
        public int Id { get; set; }
        public int TemplateId { get; set; }
        public string? TemplatePath { get; set; }

        public string? FullName { get; set; }
        public string? JobTitle { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? Summary { get; set; }
        public string? AvatarPath { get; set; }
        public string? Website { get; set; }

        // Các mục bạn đã có
        public List<ExperienceVM> Experiences { get; set; } = new List<ExperienceVM>();
        public List<EducationVM> Educations { get; set; } = new List<EducationVM>();

        // THÊM CÁC DÒNG NÀY ĐỂ HẾT BÁO ĐỎ Ở T1.CSHTML
        public List<LanguageVM> Languages { get; set; } = new List<LanguageVM>();
        public List<ITSkillVM> ITSKills { get; set; } = new List<ITSkillVM>();
        public List<ReferenceVM> References { get; set; } = new List<ReferenceVM>();

        public string? Interests { get; set; }     // Sở thích
        public string? Activities { get; set; }    // Hoạt động
        public string? Certifications { get; set; } // Chứng chỉ
    }

    // Các Class VM bổ sung
    public class ExperienceVM
    {
        public int Id { get; set; }
        public string? Company { get; set; }
        public string? Role { get; set; }
        public string? Duration { get; set; }
        public string? Description { get; set; }
        public int EditCVViewModelId { get; set; }
    }

    public class EducationVM
    {
        public int Id { get; set; }
        public string? School { get; set; }
        public string? Major { get; set; }
        public string? Duration { get; set; }
        public int EditCVViewModelId { get; set; }
    }

    // Class cho Ngoại ngữ
    public class LanguageVM
    {
        public int Id { get; set; }
        public string? LanguageName { get; set; }
        public string? Level { get; set; } // Ví dụ: Giỏi, Khá
        public int EditCVViewModelId { get; set; }
    }

    // Class cho Tin học
    public class ITSkillVM
    {
        public int Id { get; set; }
        public string? SkillName { get; set; }
        public string? Description { get; set; }
        public int EditCVViewModelId { get; set; }
    }

    // Class cho Người liên hệ
    public class ReferenceVM
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Position { get; set; }
        public string? Company { get; set; }
        public string? Phone { get; set; }
        public int EditCVViewModelId { get; set; }
    }
}
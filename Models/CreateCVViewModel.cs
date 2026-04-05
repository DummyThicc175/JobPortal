using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace JobPortal.Models
{
    public class CreateCVViewModel
    {
        public int TemplateId { get; set; }

        [Required]
        public string TemplateViewPath { get; set; } = "";

        [Display(Name = "Họ và tên")]
        public string? FullName { get; set; }

        [EmailAddress]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [Phone]
        [Display(Name = "Số điện thoại")]
        public string? Phone { get; set; }

        [Display(Name = "Tiêu đề CV")]
        public string? Title { get; set; }

        [Display(Name = "Tóm tắt bản thân")]
        public string? Summary { get; set; }

        public List<EducationItem>? Educations { get; set; }
        public List<ExperienceItem>? Experiences { get; set; }
        public List<string>? Skills { get; set; }
    }

    public class EducationItem
    {
        public string? School { get; set; }
        public string? Degree { get; set; }
        public string? Period { get; set; }
    }

    public class ExperienceItem
    {
        public string? Company { get; set; }
        public string? Role { get; set; }
        public string? Period { get; set; }
        public string? Description { get; set; }
    }
}

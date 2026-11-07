using System.ComponentModel.DataAnnotations;

namespace JobPortal.Models
{
    public class CVTemplate
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = "";

        public string? Category { get; set; }

        // đường dẫn thumbnail under wwwroot (ví dụ: images/templates/1.jpg)
        public string? Thumbnail { get; set; }

        // đường dẫn tới Razor partial / static HTML template để render preview (ví dụ: Views/CVTemplates/Template1.cshtml hoặc /templates/t1.html)
        [Required]
        public string TemplateViewPath { get; set; } = "";
    }
}

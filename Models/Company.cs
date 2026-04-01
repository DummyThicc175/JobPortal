using System.ComponentModel.DataAnnotations;

namespace JobPortal.Models
{
    public class Company
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên công ty là bắt buộc")]
        [Display(Name = "Tên công ty")]
        public string Name { get; set; }

        [Display(Name = "Mô tả/Văn hóa công ty")]
        public string Description { get; set; }

        public string LogoUrl { get; set; } // Lưu đường dẫn ảnh logo

        public string Website { get; set; }

        public string Address { get; set; }

        // Liên kết với tài khoản Identity của Employer
        public string EmployerId { get; set; } 
    }
}
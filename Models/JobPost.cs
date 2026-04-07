using JobPortal.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobPortal.Models
{
    public class JobPost
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tiêu đề không được để trống")]
        public string Title { get; set; }

        public string? Description { get; set; }

        // Tên công ty (dùng khi không có liên kết bảng Company)
        public string? CompanyName { get; set; }

        public string? SalaryRange { get; set; } // Mức lương (Ví dụ: "10 - 15")
       
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public bool IsApproved { get; set; } = false;

        public string? EmployerId { get; set; }

        [Display(Name = "Ngành nghề")]
        public string? CategoryName { get; set; }

        // Liên kết với bảng Ngành nghề (Category)
        public int? CategoryId { get; set; }
        public virtual Category? Category { get; set; }

        // --- LIÊN KẾT VỚI BẢNG CÔNG TY (HẾT LỖI ĐỎ) ---
        public int? CompanyId { get; set; } // Khóa ngoại

        [ForeignKey("CompanyId")]
        public virtual Company? Company { get; set; } // Thuộc tính điều hướng
        [Display(Name = "Địa điểm")]
        public int? LocationId { get; set; }

        [ForeignKey("LocationId")]
        public virtual Location? JobLocation { get; set; }
    }
}
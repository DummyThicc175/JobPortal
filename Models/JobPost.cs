using JobPortal.Models.JobPortal.Models;
using System.ComponentModel.DataAnnotations;

namespace JobPortal.Models
{
    public class JobPost
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "Tiêu đề không được để trống")]
        public string Title { get; set; }
        public string Description { get; set; }
        public string CompanyName { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool IsApproved { get; set; } = false; // Admin sẽ duyệt cái này
        public string EmployerId { get; set; } // Liên kết với tài khoản nhà tuyển dụng
        [Display(Name = "Ngành nghề")]
        public int? CategoryId { get; set; }
        public virtual Category? Category { get; set; }
    }
}

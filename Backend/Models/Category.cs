using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace JobPortal.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên ngành nghề không được để trống")]
        [Display(Name = "Tên ngành nghề")]
        [MaxLength(256)]
        public string Name { get; set; } = "";

        // Quan hệ 1-n: Một ngành nghề có nhiều tin tuyển dụng
        public virtual ICollection<JobPost> JobPosts { get; set; } = new List<JobPost>();
    }
}

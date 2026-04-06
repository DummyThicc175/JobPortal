using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace JobPortal.Models
{
    public class Location
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên tỉnh/thành phố không được để trống")]
        [Display(Name = "Tỉnh/Thành phố")]
        public string Name { get; set; } // Ví dụ: Hà Nội, TP. HCM...

        public virtual ICollection<JobPost> JobPosts { get; set; } = new List<JobPost>();
    }
}
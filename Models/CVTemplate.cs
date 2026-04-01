namespace JobPortal.Models
{
    public class CVTemplate
    {

        public int Id { get; set; }
        public string Name { get; set; } // Tên mẫu: Hiện đại, Sáng tạo...
        public string Thumbnail { get; set; } // Link ảnh đại diện mẫu CV
        public string Category { get; set; } // Phân loại để lọc
    }
}

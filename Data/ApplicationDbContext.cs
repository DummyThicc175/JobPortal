using JobPortal.Models; // Chỉ cần dùng một cái này nếu namespace các model chuẩn
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using JobPortal.Models;

namespace JobPortal.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // --- Các bảng chính ---
        public DbSet<JobPost> JobPosts { get; set; }
        public DbSet<Company> Companies { get; set; }
        public DbSet<CV> CVs { get; set; }
        public DbSet<Application> Applications { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<CVTemplate> CVTemplates { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Location> Locations { get; set; }

        // --- Các bảng lưu trữ thông tin CV thực tế ---
        // Lưu ý: Tôi đã đổi tên từ ViewModel sang tên bảng thực tế để tránh nhầm lẫn
        // Bạn cần đảm bảo các Class này tồn tại trong Models
        public DbSet<EditCVViewModel> UserCVs { get; set; }
        public DbSet<ExperienceVM> Experiences { get; set; }
        public DbSet<EducationVM> Educations { get; set; }
        public DbSet<LanguageVM> Languages { get; set; }
        public DbSet<ITSkillVM> ITSKills { get; set; }
        public DbSet<ReferenceVM> References { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Seed dữ liệu cho Location
            modelBuilder.Entity<Location>().HasData(
                new Location { Id = 1, Name = "An Giang" },
                new Location { Id = 2, Name = "Bà Rịa - Vũng Tàu" },
                new Location { Id = 3, Name = "Bắc Giang" },
                new Location { Id = 4, Name = "Bắc Kạn" },
                new Location { Id = 5, Name = "Bạc Liêu" },
                new Location { Id = 6, Name = "Bắc Ninh" },
                new Location { Id = 7, Name = "Bến Tre" },
                new Location { Id = 8, Name = "Bình Định" },
                new Location { Id = 9, Name = "Bình Dương" },
                new Location { Id = 10, Name = "Bình Phước" },
                new Location { Id = 11, Name = "Bình Thuận" },
                new Location { Id = 12, Name = "Cà Mau" },
                new Location { Id = 13, Name = "Cần Thơ" },
                new Location { Id = 14, Name = "Cao Bằng" },
                new Location { Id = 15, Name = "Đà Nẵng" },
                new Location { Id = 16, Name = "Đắk Lắk" },
                new Location { Id = 17, Name = "Đắk Nông" },
                new Location { Id = 18, Name = "Điện Biên" },
                new Location { Id = 19, Name = "Đồng Nai" },
                new Location { Id = 20, Name = "Đồng Tháp" },
                new Location { Id = 21, Name = "Gia Lai" },
                new Location { Id = 22, Name = "Hà Giang" },
                new Location { Id = 23, Name = "Hà Nam" },
                new Location { Id = 24, Name = "Hà Nội" },
                new Location { Id = 25, Name = "Hà Tĩnh" },
                new Location { Id = 26, Name = "Hải Dương" },
                new Location { Id = 27, Name = "Hải Phòng" },
                new Location { Id = 28, Name = "Hậu Giang" },
                new Location { Id = 29, Name = "Hòa Bình" },
                new Location { Id = 30, Name = "Hưng Yên" },
                new Location { Id = 31, Name = "Khánh Hòa" },
                new Location { Id = 32, Name = "Kiên Giang" },
                new Location { Id = 33, Name = "Kon Tum" },
                new Location { Id = 34, Name = "Lai Châu" },
                new Location { Id = 35, Name = "Lâm Đồng" },
                new Location { Id = 36, Name = "Lạng Sơn" },
                new Location { Id = 37, Name = "Lào Cai" },
                new Location { Id = 38, Name = "Long An" },
                new Location { Id = 39, Name = "Nam Định" },
                new Location { Id = 40, Name = "Nghệ An" },
                new Location { Id = 41, Name = "Ninh Bình" },
                new Location { Id = 42, Name = "Ninh Thuận" },
                new Location { Id = 43, Name = "Phú Thọ" },
                new Location { Id = 44, Name = "Phú Yên" },
                new Location { Id = 45, Name = "Quảng Bình" },
                new Location { Id = 46, Name = "Quảng Nam" },
                new Location { Id = 47, Name = "Quảng Ngãi" },
                new Location { Id = 48, Name = "Quảng Ninh" },
                new Location { Id = 49, Name = "Quảng Trị" },
                new Location { Id = 50, Name = "Sóc Trăng" },
                new Location { Id = 51, Name = "Sơn La" },
                new Location { Id = 52, Name = "Tây Ninh" },
                new Location { Id = 53, Name = "Thái Bình" },
                new Location { Id = 54, Name = "Thái Nguyên" },
                new Location { Id = 55, Name = "Thanh Hóa" },
                new Location { Id = 56, Name = "Thừa Thiên Huế" },
                new Location { Id = 57, Name = "Tiền Giang" },
                new Location { Id = 58, Name = "TP. Hồ Chí Minh" },
                new Location { Id = 59, Name = "Trà Vinh" },
                new Location { Id = 60, Name = "Tuyên Quang" },
                new Location { Id = 61, Name = "Vĩnh Long" },
                new Location { Id = 62, Name = "Vĩnh Phúc" },
                new Location { Id = 63, Name = "Yên Bái" }
            );

            // Seed dữ liệu mẫu cho CVTemplate
            modelBuilder.Entity<CVTemplate>().HasData(
                new CVTemplate
                {
                    Id = 1,
                    Name = "Mẫu 1",
                    Category = "Hiện đại",
                    Thumbnail = "images/templates/t1.jpg",
                    TemplateViewPath = "~/Areas/Candidate/Views/Home/Template_1.cshtml"
                }
            );
        }
    }
}
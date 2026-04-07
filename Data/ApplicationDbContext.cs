using JobPortal.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

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
        public DbSet<EditCVViewModel> UserCVs { get; set; }
        public DbSet<ExperienceVM> Experiences { get; set; }
        public DbSet<EducationVM> Educations { get; set; }
        public DbSet<LanguageVM> Languages { get; set; }
        public DbSet<ITSkillVM> ITSKills { get; set; }
        public DbSet<ReferenceVM> References { get; set; }
        public DbSet<JobPortal.Models.Candidate> Candidates { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 1. Tạo các Role mặc định
            // Lưu ý: Trong thực tế nên dùng ID cố định thay vì Guid.NewGuid() để tránh tạo lại ID mới mỗi lần chạy migration
            string adminRoleId = "8505d10d-7730-4e57-96fd-99335279b974";
            string employerRoleId = "02174cfc-2dc1-460d-8521-396c0966a4f0";
            string candidateRoleId = "7d9b7113-a8f8-4035-99a7-a20dd400f6a3";

            modelBuilder.Entity<IdentityRole>().HasData(
                new IdentityRole { Id = adminRoleId, Name = "Admin", NormalizedName = "ADMIN" },
                new IdentityRole { Id = employerRoleId, Name = "Employer", NormalizedName = "EMPLOYER" },
                new IdentityRole { Id = candidateRoleId, Name = "Candidate", NormalizedName = "CANDIDATE" }
            );

            // 2. Tạo User và mã hóa mật khẩu
            var hasher = new PasswordHasher<IdentityUser>();

            string adminUserId = "b74ddd14-6340-4840-95c2-db12554843e5";
            string employerUserId = "c9284cfc-2dc1-460d-8521-396c0966a4f1";
            string candidateUserId = "d83b7113-a8f8-4035-99a7-a20dd400f6a4";

            // Tạo tk Admin
            var adminUser = new IdentityUser
            {
                Id = adminUserId,
                UserName = "admin@jobhub.com",
                NormalizedUserName = "ADMIN@JOBHUB.COM",
                Email = "admin@jobhub.com",
                NormalizedEmail = "ADMIN@JOBHUB.COM",
                EmailConfirmed = true
            };
            adminUser.PasswordHash = hasher.HashPassword(adminUser, "Admin@123");

            // Tạo tk Employer
            var employerUser = new IdentityUser
            {
                Id = employerUserId,
                UserName = "employer@jobhub.com",
                NormalizedUserName = "EMPLOYER@JOBHUB.COM",
                Email = "employer@jobhub.com",
                NormalizedEmail = "EMPLOYER@JOBHUB.COM",
                EmailConfirmed = true
            };
            employerUser.PasswordHash = hasher.HashPassword(employerUser, "Employer@123");

            // Tạo tk Candidate
            var candidateUser = new IdentityUser
            {
                Id = candidateUserId,
                UserName = "candidate@jobhub.com",
                NormalizedUserName = "CANDIDATE@JOBHUB.COM",
                Email = "candidate@jobhub.com",
                NormalizedEmail = "CANDIDATE@JOBHUB.COM",
                EmailConfirmed = true
            };
            candidateUser.PasswordHash = hasher.HashPassword(candidateUser, "Candidate@123");

            // Đưa User vào DB - Sửa builder thành modelBuilder
            modelBuilder.Entity<IdentityUser>().HasData(adminUser, employerUser, candidateUser);

            // 3. Gán Role cho User - Sửa builder thành modelBuilder
            modelBuilder.Entity<IdentityUserRole<string>>().HasData(
                new IdentityUserRole<string> { RoleId = adminRoleId, UserId = adminUserId },
                new IdentityUserRole<string> { RoleId = employerRoleId, UserId = employerUserId },
                new IdentityUserRole<string> { RoleId = candidateRoleId, UserId = candidateUserId }
            );

            // Seed dữ liệu cho Location (giữ nguyên)
            // Seed dữ liệu cho Location - ĐÃ KHÔI PHỤC ĐẦY ĐỦ
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

            // Seed dữ liệu mẫu cho CVTemplate (giữ nguyên)
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
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
        public DbSet<InterviewSession> InterviewSessions { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<CVTemplate> CVTemplates { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Location> Locations { get; set; }

        // --- Các bảng lưu trữ thông tin CV thực tế ---
        
        public DbSet<CvExperience> Experiences { get; set; }
        public DbSet<CvEducation> Educations { get; set; }
        public DbSet<CvLanguage> Languages { get; set; }
        public DbSet<CvSkill> ITSKills { get; set; }
        public DbSet<CvReference> References { get; set; }
        public DbSet<JobPortal.Models.Candidate> Candidates { get; set; }
        public DbSet<SavedJob> SavedJobs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // Keep Identity key lengths identical in the web host, migration tools and maintenance runners.
            modelBuilder.Entity<IdentityUserLogin<string>>(entity =>
            {
                entity.Property(login => login.LoginProvider).HasMaxLength(128);
                entity.Property(login => login.ProviderKey).HasMaxLength(128);
            });
            modelBuilder.Entity<IdentityUserToken<string>>(entity =>
            {
                entity.Property(token => token.LoginProvider).HasMaxLength(128);
                entity.Property(token => token.Name).HasMaxLength(128);
            });

            modelBuilder.Entity<Candidate>(entity =>
            {
                entity.Property(c => c.UserId).HasMaxLength(450);
                entity.Property(c => c.FullName).HasMaxLength(200);
                entity.Property(c => c.Phone).HasMaxLength(30);
                entity.Property(c => c.Gender).HasMaxLength(20);
                entity.Property(c => c.AvatarUrl).HasMaxLength(500);
                entity.Property(c => c.Address).HasMaxLength(500);
                entity.HasIndex(c => c.UserId).IsUnique();
                entity.HasOne(c => c.User).WithOne().HasForeignKey<Candidate>(c => c.UserId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(c => c.DefaultCV).WithMany().HasForeignKey(c => c.DefaultCVId).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<CV>(entity =>
            {
                entity.HasOne<IdentityUser>().WithMany().HasForeignKey(c => c.CandidateId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(c => c.Template).WithMany().HasForeignKey(c => c.TemplateId).OnDelete(DeleteBehavior.Restrict);
                entity.HasIndex(c => new { c.CandidateId, c.IsSnapshot, c.UploadDate });
                entity.HasIndex(c => c.FileName);
                entity.ToTable("CVs", t => t.HasCheckConstraint("CK_CVs_AiScore", "[AiScore] IS NULL OR ([AiScore] >= 0 AND [AiScore] <= 100)"));
            });
            modelBuilder.Entity<CvExperience>().HasBaseType((Type?)null).ToTable("Experiences");
            modelBuilder.Entity<CvEducation>().HasBaseType((Type?)null).ToTable("Educations");
            modelBuilder.Entity<CvSkill>().HasBaseType((Type?)null).ToTable("ITSKills");
            modelBuilder.Entity<CvLanguage>().HasBaseType((Type?)null).ToTable("Languages");
            modelBuilder.Entity<CvReference>().HasBaseType((Type?)null).ToTable("References");
            modelBuilder.Entity<CvExperience>().HasOne<CV>().WithMany(c => c.Experiences).HasForeignKey(s => s.CVId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<CvEducation>().HasOne<CV>().WithMany(c => c.Educations).HasForeignKey(s => s.CVId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<CvSkill>().HasOne<CV>().WithMany(c => c.Skills).HasForeignKey(s => s.CVId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<CvLanguage>().HasOne<CV>().WithMany(c => c.Languages).HasForeignKey(s => s.CVId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<CvReference>().HasOne<CV>().WithMany(c => c.References).HasForeignKey(s => s.CVId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Application>(entity =>
            {
                entity.HasIndex(a => new { a.CandidateId, a.JobPostId }).IsUnique();
                entity.HasOne<IdentityUser>().WithMany().HasForeignKey(a => a.CandidateId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(a => a.JobPost).WithMany().HasForeignKey(a => a.JobPostId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(a => a.CV).WithMany().HasForeignKey(a => a.CVId).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<InterviewSession>(entity =>
            {
                entity.HasOne(s => s.Application).WithMany(a => a.InterviewSessions)
                    .HasForeignKey(s => s.ApplicationId).OnDelete(DeleteBehavior.Restrict);
                entity.HasIndex(s => s.ApplicationId).IsUnique().HasFilter("[EndedAt] IS NULL");
            });
            modelBuilder.Entity<SavedJob>(entity =>
            {
                entity.HasIndex(s => new { s.CandidateId, s.JobPostId }).IsUnique();
                entity.HasOne<IdentityUser>().WithMany().HasForeignKey(s => s.CandidateId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(s => s.JobPost).WithMany().HasForeignKey(s => s.JobPostId).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<Company>(entity =>
            {
                entity.HasOne<IdentityUser>().WithMany().HasForeignKey(c => c.EmployerId).OnDelete(DeleteBehavior.Restrict);
                entity.HasIndex(c => new { c.EmployerId, c.Name }).IsUnique();
            });
            modelBuilder.Entity<JobPost>(entity =>
            {
                entity.Property(j => j.EmployerId).HasMaxLength(450);
                entity.HasOne<IdentityUser>().WithMany().HasForeignKey(j => j.EmployerId).OnDelete(DeleteBehavior.Restrict);
                entity.HasIndex(j => new { j.IsApproved, j.CreatedAt });
                entity.HasIndex(j => new { j.EmployerId, j.CreatedAt });
            });
            modelBuilder.Entity<Category>().HasIndex(c => c.Name).IsUnique();
            modelBuilder.Entity<Location>().HasIndex(l => l.Name).IsUnique();
            modelBuilder.Entity<ChatMessage>(entity =>
            {
                entity.HasOne<IdentityUser>().WithMany().HasForeignKey(m => m.SenderId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne<IdentityUser>().WithMany().HasForeignKey(m => m.ReceiverId).OnDelete(DeleteBehavior.Restrict);
                entity.HasIndex(m => new { m.SenderId, m.ReceiverId, m.Id });
                entity.HasIndex(m => new { m.ReceiverId, m.IsRead });
            });
            modelBuilder.Entity<CVTemplate>(entity =>
            {
                entity.Property(t => t.Name).HasMaxLength(200);
                entity.Property(t => t.Category).HasMaxLength(50);
                entity.Property(t => t.Thumbnail).HasMaxLength(500);
                entity.Property(t => t.TemplateViewPath).HasMaxLength(500);
            });

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
            // Stable seed values prevent unrelated password/stamp updates in new migrations.

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
                EmailConfirmed = true, LockoutEnabled = true
            };
            adminUser.SecurityStamp = "9ee6378f-ef6d-40b9-8a22-279d7bcec5b5";
            adminUser.ConcurrencyStamp = "baa10bc3-20bf-4d89-8907-78dc82d7bbeb";
            adminUser.PasswordHash = "AQAAAAIAAYagAAAAEJgcDjpbg8ztKCooFYOYfNn0fm4S4rqi8tIe/C1f4KSNlyQtoOV5nEjtAm860P3MUA==";

            // Tạo tk Employer
            var employerUser = new IdentityUser
            {
                Id = employerUserId,
                UserName = "employer@jobhub.com",
                NormalizedUserName = "EMPLOYER@JOBHUB.COM",
                Email = "employer@jobhub.com",
                NormalizedEmail = "EMPLOYER@JOBHUB.COM",
                EmailConfirmed = true, LockoutEnabled = true
            };
            employerUser.SecurityStamp = "ec39a2f2-61d1-4655-bbc6-cb9dddda1c15";
            employerUser.ConcurrencyStamp = "8bb6a4c8-22f7-4f22-bb46-f1a225a56b2b";
            employerUser.PasswordHash = "AQAAAAIAAYagAAAAEMulEQM985vjfBLoozHC3hJX+1IxlyjQ16Kp37GJ+6hE38QtMQuYfzRNGIbn/Tj+FA==";

            // Tạo tk Candidate
            var candidateUser = new IdentityUser
            {
                Id = candidateUserId,
                UserName = "candidate@jobhub.com",
                NormalizedUserName = "CANDIDATE@JOBHUB.COM",
                Email = "candidate@jobhub.com",
                NormalizedEmail = "CANDIDATE@JOBHUB.COM",
                EmailConfirmed = true, LockoutEnabled = true
            };
            candidateUser.SecurityStamp = "2e75ba8e-6545-4545-8770-ae35abe5ec44";
            candidateUser.ConcurrencyStamp = "298460db-13f3-427a-8cc2-74e6421fea2c";
            candidateUser.PasswordHash = "AQAAAAIAAYagAAAAEOjhd8FhUHCYScZqp7C3c1XB3GwlFOvT2iMT2i2qqYlpR41/DIGHo7JiE28nPsGJ7g==";

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
                    Name = "Mẫu Thiết Kế Truyền Thống",
                    Category = "Popular",
                    Thumbnail = "images/templates/t1.jpg",
                    TemplateViewPath = "Template1"
                },
                new CVTemplate
                {
                    Id = 2,
                    Name = "Mẫu Công Nghệ Hiện Đại",
                    Category = "Creative",
                    Thumbnail = "images/templates/t2.jpg",
                    TemplateViewPath = "Template2"
                }
            );
        }
    }
}

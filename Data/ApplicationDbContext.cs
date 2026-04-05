using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using JobPortal.Models; // Sửa tên JobPortal thành tên dự án của bạn nếu khác

namespace JobPortal.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<JobPost> JobPosts { get; set; }
        public DbSet<Company> Companies { get; set; }
        public DbSet<CV> CVs { get; set; }
        public DbSet<Application> Applications { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }

        public DbSet<CVTemplate> CVTemplates { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<CVTemplate>().HasData(
                new CVTemplate {
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
using JobPortal.Models;
using JobPortal.Models.JobPortal.Models;
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

        // Các bảng chính
        public DbSet<JobPost> JobPosts { get; set; }
        public DbSet<Company> Companies { get; set; }
        public DbSet<CV> CVs { get; set; }
        public DbSet<Application> Applications { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<CVTemplate> CVTemplates { get; set; }
        public DbSet<Category> Categories { get; set; }

        // Các bảng liên quan đến CV (ViewModels/Entities)
        public DbSet<EditCVViewModel> UserCVs { get; set; }
        public DbSet<ExperienceVM> Experiences { get; set; }
        public DbSet<EducationVM> Educations { get; set; }
        public DbSet<LanguageVM> Languages { get; set; }
        public DbSet<ITSkillVM> ITSKills { get; set; }
        public DbSet<ReferenceVM> References { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

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
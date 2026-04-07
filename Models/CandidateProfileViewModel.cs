using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;

namespace JobPortal.Models
{
    public class CandidateProfileViewModel
    {
        // --- Hồ sơ cá nhân ---
        public string FullName { get; set; } 
        public string Email { get; set; }
        public string? Phone { get; set; }
        public string? Gender { get; set; }
        public DateTime? Birthday { get; set; }
        public string? Address { get; set; }
        public string? MaritalStatus { get; set; } // Thêm dòng này vào dưới Address

        // --- Xử lý Ảnh đại diện ---
        public string? AvatarUrl { get; set; } // Đường dẫn để hiển thị ảnh
        public IFormFile? AvatarFile { get; set; } // File thực tế khi upload

        public IFormFile? CVFile { get; set; } // File CV thực tế (.pdf, .doc) khi upload
        public string? CVUrl { get; set; }     // Đường dẫn file CV trong database

        public int CompanyId { get; set; } // Khóa ngoại
        public virtual Company Company { get; set; } // Thuộc tính điều hướng

        public List<Category>? Categories { get; set; }

        // --- Tiêu chí tìm việc ---
        public string Industry { get; set; } 
        public string DesiredLocation { get; set; }
        public string ExpectedSalary { get; set; } 

        // --- Thông tin chung ---
        public string ExperienceYear { get; set; } 
        public string? EducationLevel { get; set; }

        // --- Cột phải: Việc làm gợi ý ---
        public List<JobPost> SuggestedJobs { get; set; } = new List<JobPost>();
    }
}
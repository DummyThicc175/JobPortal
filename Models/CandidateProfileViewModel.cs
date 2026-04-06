using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;

namespace JobPortal.Models
{
    public class CandidateProfileViewModel
    {
        // --- Hồ sơ cá nhân ---
        public string FullName { get; set; } = "0083_Trần Thạch Thảo Anh";
        public string Email { get; set; } = "thaoanhtranthach@gmail.com";
        public string? Phone { get; set; }
        public string? Gender { get; set; }
        public DateTime? Birthday { get; set; }
        public string? Address { get; set; }

        // --- Xử lý Ảnh đại diện ---
        public string? AvatarUrl { get; set; } // Đường dẫn để hiển thị ảnh
        public IFormFile? AvatarFile { get; set; } // File thực tế khi upload

        public int CompanyId { get; set; } // Khóa ngoại
        public virtual Company Company { get; set; } // Thuộc tính điều hướng

        // --- Tiêu chí tìm việc ---
        public string Industry { get; set; } = "IT Phần cứng - Mạng";
        public string DesiredLocation { get; set; } = "Đồng Nai";
        public string ExpectedSalary { get; set; } = "Thoả thuận";

        // --- Thông tin chung ---
        public string ExperienceYear { get; set; } = "Chưa có kinh nghiệm";
        public string? EducationLevel { get; set; }

        // --- Cột phải: Việc làm gợi ý ---
        public List<JobPost> SuggestedJobs { get; set; } = new List<JobPost>();
    }
}
using JobPortal.Data;
using JobPortal.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // Thêm cái này để dùng async/await tốt hơn

namespace JobPortal.Areas.Candidate.Controllers
{
    [Area("Candidate")]
    public class JobController : Controller


    {

        private readonly UserManager<IdentityUser> _userManager;
        private readonly ApplicationDbContext _context;

        public JobController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager; 
        }

        [HttpPost]
        [ValidateAntiForgeryToken] // Bảo mật hơn cho Form
        public async Task<IActionResult> Apply(int jobId, string candidateId)
        {
            if (string.IsNullOrEmpty(candidateId) || jobId <= 0)
            {
                TempData["Error"] = "Thông tin ứng tuyển không hợp lệ. Vui lòng đăng nhập!";
                return RedirectToAction("Index", "Home", new { area = "Candidate" });
            }

            var application = new Application
            {
                JobPostId = jobId,
                CandidateId = candidateId,
                AppliedDate = DateTime.Now,
                Status = ApplicationStatus.Submitted
            };

            try
            {
                _context.Applications.Add(application);
                await _context.SaveChangesAsync();
                TempData["Message"] = "Bạn đã ứng tuyển thành công!";
            }
            catch (Exception)
            {
                TempData["Error"] = "Có lỗi xảy ra khi gửi đơn ứng tuyển.";
            }

            return RedirectToAction("Index", "Home", new { area = "Candidate" });
        }

        // Action để hiển thị trang chi tiết công việc
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var job = await _context.JobPosts.FirstOrDefaultAsync(j => j.Id == id);

            if (job == null)
            {
                return NotFound();
            }

            // Trả về view ở thư mục root Views nếu bạn không muốn di chuyển file
            return View("~/Areas/Candidate/Views/Job/Details.cshtml", job);
        }


        
    }
}
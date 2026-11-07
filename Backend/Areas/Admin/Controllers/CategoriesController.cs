using JobPortal.Data;
using JobPortal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JobPortal.Infrastructure;

namespace JobPortal.Areas.Admin.Controllers
{
    [Area("Admin")] // Khai báo thuộc tính vùng
    [Authorize(Roles = "Admin")]
    public class CategoriesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CategoriesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Categories
        public async Task<IActionResult> Index(string? searchTerm, int page = 1, CancellationToken token = default)
        {
            var categories = _context.Categories.AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                categories = categories.Where(c => c.Name.Contains(searchTerm));
            }

            return View(await categories.AsNoTracking().OrderBy(c => c.Name).PageAsync(this, page, token));
        }

        // GET: Admin/Categories/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _context.Categories
                .FirstOrDefaultAsync(m => m.Id == id);
            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }

        // GET: Admin/Categories/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/Categories/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name")] Category category)
        {
            category.Id = 0;
            category.Name = category.Name?.Trim() ?? "";
            if (await _context.Categories.AnyAsync(c => c.Name == category.Name)) ModelState.AddModelError(nameof(category.Name), "Ngành nghề đã tồn tại.");
            if (ModelState.IsValid)
            {
                _context.Add(category);
                try { await _context.SaveChangesAsync(); }
                catch (DbUpdateException exception) when (DbErrors.IsUniqueViolation(exception))
                { ModelState.AddModelError(nameof(category.Name), "Ngành nghề đã tồn tại."); return View(category); }
                return RedirectToAction(nameof(Index));
            }
            return View(category);
        }

        // GET: Admin/Categories/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }
            return View(category);
        }

        // POST: Admin/Categories/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name")] Category category)
        {
            category.Name = category.Name?.Trim() ?? "";
            if (await _context.Categories.AnyAsync(c => c.Id != id && c.Name == category.Name)) ModelState.AddModelError(nameof(category.Name), "Ngành nghề đã tồn tại.");
            if (id != category.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(category);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CategoryExists(category.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (DbUpdateException exception) when (DbErrors.IsUniqueViolation(exception))
                { ModelState.AddModelError(nameof(category.Name), "Ngành nghề đã tồn tại."); return View(category); }
                return RedirectToAction(nameof(Index));
            }
            return View(category);
        }

        // GET: Admin/Categories/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _context.Categories
                .FirstOrDefaultAsync(m => m.Id == id);
            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }

        // POST: Admin/Categories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category != null)
            {
                if (await _context.JobPosts.AnyAsync(j => j.CategoryId == id))
                { TempData["Error"] = "Ngành nghề đang được tin tuyển dụng sử dụng, không thể xóa."; return RedirectToAction(nameof(Index)); }
                _context.Categories.Remove(category);
            }

            try { await _context.SaveChangesAsync(); }
            catch (DbUpdateException)
            { TempData["Error"] = "Ngành nghề đang được sử dụng, không thể xóa."; }
            return RedirectToAction(nameof(Index));
        }

        private bool CategoryExists(int id)
        {
            return _context.Categories.Any(e => e.Id == id);
        }
    }
}

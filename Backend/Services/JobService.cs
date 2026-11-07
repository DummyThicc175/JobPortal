using JobPortal.Data;
using JobPortal.Models;
using Microsoft.EntityFrameworkCore;

namespace JobPortal.Services;

public sealed class JobService(ApplicationDbContext database)
{
    public IQueryable<JobPost> TimKiem(string? keyword, int? locationId)
    {
        var query = database.JobPosts.AsNoTracking().Include(j => j.Company).Include(j => j.Category).Include(j => j.JobLocation)
            .Where(j => j.IsApproved);
        keyword = keyword?.Trim();
        if (!string.IsNullOrEmpty(keyword)) query = query.Where(j => j.Title.Contains(keyword) || (j.Company != null && j.Company.Name.Contains(keyword)));
        if (locationId.HasValue) query = query.Where(j => j.LocationId == locationId);
        return query.OrderByDescending(j => j.CreatedAt).ThenByDescending(j => j.Id);
    }

    public async Task<Company> LayHoacTaoCongTyAsync(string employerId, string name, CancellationToken token)
    {
        name = name.Trim();
        var normalized = name.ToUpperInvariant();
        var company = await database.Companies.SingleOrDefaultAsync(c => c.EmployerId == employerId && c.Name.ToUpper() == normalized, token);
        if (company != null) return company;
        company = new Company { EmployerId = employerId, Name = name };
        database.Companies.Add(company);
        return company;
    }

    public async Task<bool> XoaHoacAnTinAsync(JobPost job, CancellationToken token)
    {
        var used = await database.Applications.AnyAsync(a => a.JobPostId == job.Id, token) || await database.SavedJobs.AnyAsync(s => s.JobPostId == job.Id, token);
        if (used) job.IsApproved = false;
        else database.JobPosts.Remove(job);
        try { await database.SaveChangesAsync(token); }
        catch (DbUpdateException) when (!used)
        {
            // A submission may have arrived between checking references and deleting.
            database.Entry(job).State = EntityState.Unchanged;
            if (!await database.Applications.AnyAsync(a => a.JobPostId == job.Id, token) && !await database.SavedJobs.AnyAsync(s => s.JobPostId == job.Id, token)) throw;
            job.IsApproved = false;
            await database.SaveChangesAsync(token);
            used = true;
        }
        return !used;
    }
}

using System.Data;
using System.Security.Claims;
using JobPortal.Data;
using JobPortal.Hubs;
using JobPortal.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace JobPortal.Services;

public sealed class InterviewService(ApplicationDbContext database, InterviewPresence presence,
    IHubContext<InterviewHub> hub, IOptions<IdentityOptions> identity)
{
    public IQueryable<InterviewSession> Sessions => database.InterviewSessions.AsNoTracking()
        .Include(s => s.Application).ThenInclude(a => a.JobPost).ThenInclude(j => j.Company);

    public async Task<InterviewSession?> LayPhienCoQuyenAsync(Guid id, ClaimsPrincipal user, CancellationToken token = default)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return null;
        var session = await Sessions.SingleOrDefaultAsync(s => s.Id == id, token);
        if (session == null || session.EndedAt != null || session.Application.Status != ApplicationStatus.Interview) return null;
        var role = session.Application.CandidateId == userId ? "Candidate" : session.Application.JobPost.EmployerId == userId ? "Employer" : null;
        if (role == null || !user.IsInRole(role)) return null;
        var account = await database.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == userId, token);
        if (account == null || account.LockoutEnd > DateTimeOffset.UtcNow || account.SecurityStamp != user.FindFirstValue(identity.Value.ClaimsIdentity.SecurityStampClaimType)) return null;
        var hasRole = await (from ur in database.UserRoles join r in database.Roles on ur.RoleId equals r.Id
            where ur.UserId == userId && r.Name == role select ur).AnyAsync(token);
        return hasRole ? session : null;
    }

    public async Task<bool> CapNhatTrangThaiAsync(int id, string employerId, ApplicationStatus status, CancellationToken token)
    {
        await using var transaction = await database.Database.BeginTransactionAsync(IsolationLevel.Serializable, token);
        var application = await database.Applications.SingleOrDefaultAsync(a => a.Id == id && a.JobPost.EmployerId == employerId, token);
        if (application == null) return false;
        var active = await database.InterviewSessions.SingleOrDefaultAsync(s => s.ApplicationId == id && s.EndedAt == null, token);
        application.Status = status;
        if (status == ApplicationStatus.Interview && active == null)
            database.InterviewSessions.Add(new InterviewSession { ApplicationId = id });
        else if (status != ApplicationStatus.Interview && active != null) active.EndedAt = DateTime.UtcNow;
        await database.SaveChangesAsync(token);
        await transaction.CommitAsync(token);
        if (active?.EndedAt != null) await ThongBaoDongPhienAsync(active.Id);
        return true;
    }

    public async Task<Guid?> TaoPhienAsync(int applicationId, string employerId, CancellationToken token)
    {
        await using var transaction = await database.Database.BeginTransactionAsync(IsolationLevel.Serializable, token);
        if (!await database.Applications.AnyAsync(a => a.Id == applicationId && a.JobPost.EmployerId == employerId && a.Status == ApplicationStatus.Interview, token)) return null;
        var session = await database.InterviewSessions.SingleOrDefaultAsync(s => s.ApplicationId == applicationId && s.EndedAt == null, token);
        if (session == null) { session = new InterviewSession { ApplicationId = applicationId }; database.InterviewSessions.Add(session); }
        await database.SaveChangesAsync(token);
        await transaction.CommitAsync(token);
        return session.Id;
    }

    public async Task<bool> KetThucPhienAsync(Guid id, string employerId, CancellationToken token)
    {
        var session = await database.InterviewSessions.SingleOrDefaultAsync(s => s.Id == id && s.Application.JobPost.EmployerId == employerId, token);
        if (session == null) return false;
        session.EndedAt ??= DateTime.UtcNow;
        await database.SaveChangesAsync(token);
        await ThongBaoDongPhienAsync(id);
        return true;
    }

    public async Task ThongBaoDongPhienAsync(Guid id)
    {
        var connections = presence.DongPhong(id);
        if (connections.Length > 0) await hub.Clients.Clients(connections).SendAsync("SessionEnded");
    }
}

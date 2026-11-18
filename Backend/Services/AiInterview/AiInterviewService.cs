using JobPortal.Data;
using JobPortal.Models;
using JobPortal.ViewModels.Candidate;
using Microsoft.EntityFrameworkCore;

namespace JobPortal.Services;

public sealed class AiInterviewService(ApplicationDbContext db, AiInterviewContextService context,
    OllamaInterviewClient ai, AiInterviewRuntime runtime)
{
    public async Task<AiInterviewIndexViewModel> LayDanhSachAsync(string userId, CancellationToken token)
    {
        var applications = await db.Applications.AsNoTracking().Where(a => a.CandidateId == userId)
            .OrderByDescending(a => a.AppliedDate).Take(100)
            .Select(a => new AiInterviewApplicationItem(a.Id, a.JobPost.Title,
                a.JobPost.Category == null ? "Chưa phân loại" : a.JobPost.Category.Name,
                a.JobPost.Company == null ? "" : a.JobPost.Company.Name)).ToListAsync(token);
        var sessions = await db.AiInterviewSessions.AsNoTracking().Where(s => s.CandidateId == userId)
            .OrderByDescending(s => s.CreatedAt).Take(50)
            .Select(s => new AiInterviewHistoryItem(s.Id, s.JobTitle, s.CreatedAt, s.EndedAt != null,
                s.Turns.Count(t => t.AnsweredAt != null), s.QuestionCount)).ToListAsync(token);
        return new(applications, sessions);
    }

    public Task<Guid> BatDauAsync(string userId, int applicationId, CancellationToken token) =>
        runtime.ChayDocQuyenAsync(async () =>
        {
            var existing = await db.AiInterviewSessions.AsNoTracking()
                .FirstOrDefaultAsync(s => s.ApplicationId == applicationId && s.CandidateId == userId && s.EndedAt == null, token);
            if (existing != null) return existing.Id;
            var application = await db.Applications.AsNoTracking()
                .Include(a => a.JobPost).ThenInclude(j => j.Category)
                .Include(a => a.JobPost).ThenInclude(j => j.Company)
                .Include(a => a.CV).ThenInclude(c => c!.Experiences)
                .Include(a => a.CV).ThenInclude(c => c!.Educations)
                .Include(a => a.CV).ThenInclude(c => c!.Skills).AsSplitQuery()
                .SingleOrDefaultAsync(a => a.Id == applicationId && a.CandidateId == userId, token);
            if (application == null) throw new AiInterviewException("Không tìm thấy hồ sơ ứng tuyển của bạn.", 404);
            var cv = context.LayNoiDungCV(application.CV);
            var session = new AiInterviewSession
            {
                ApplicationId = application.Id, CandidateId = userId,
                JobTitle = application.JobPost.Title,
                CategoryName = application.JobPost.Category?.Name ?? "Chưa phân loại",
                CompanyName = application.JobPost.Company?.Name ?? "",
                JobContext = AiInterviewContextService.RutGonVanBan(application.JobPost.Description, 1800),
                CvContext = cv.Text, HasCvContext = cv.Available
            };
            var result = await ai.TaoLuotAsync(session, null, token);
            session.Turns.Add(new AiInterviewTurn { Number = 1, Question = result.Question, ExpectedPoints = result.ExpectedPoints });
            db.AiInterviewSessions.Add(session);
            try { await db.SaveChangesAsync(token); }
            catch (DbUpdateException)
            {
                // A second web instance may have started the same application while AI was running.
                db.ChangeTracker.Clear();
                var other = await db.AiInterviewSessions.AsNoTracking()
                    .FirstOrDefaultAsync(s => s.ApplicationId == applicationId && s.CandidateId == userId && s.EndedAt == null, token);
                if (other == null) throw;
                return other.Id;
            }
            return session.Id;
        }, token);

    public async Task<AiInterviewSession?> LayPhienAsync(Guid id, string userId, CancellationToken token) =>
        await db.AiInterviewSessions.AsNoTracking().Include(s => s.Turns)
            .SingleOrDefaultAsync(s => s.Id == id && s.CandidateId == userId, token);

    public async Task KiemTraLuotTraLoiAsync(Guid id, string userId, AiInterviewAnswerInput input, CancellationToken token)
    {
        var session = await LayPhienAsync(id, userId, token);
        KiemTraCauTraLoi(session, input);
    }

    public Task<bool> TraLoiAsync(Guid id, string userId, AiInterviewAnswerInput input, CancellationToken token) =>
        runtime.ChayDocQuyenAsync(async () =>
        {
            var session = await db.AiInterviewSessions.Include(s => s.Turns)
                .SingleOrDefaultAsync(s => s.Id == id && s.CandidateId == userId, token);
            var turn = KiemTraCauTraLoi(session, input);
            var answer = input.Answer.Trim();
            var result = await ai.TaoLuotAsync(session!, answer, token);
            turn.Answer = answer; turn.AnsweredAt = DateTime.UtcNow; turn.Score = result.Score;
            turn.Feedback = result.Feedback; turn.Improvement = result.Improvement; turn.Version = Guid.NewGuid();
            session!.Version = Guid.NewGuid();
            if (turn.Number == session.QuestionCount) session.EndedAt = DateTime.UtcNow;
            else session.Turns.Add(new AiInterviewTurn
            {
                Number = turn.Number + 1, Question = result.Question, ExpectedPoints = result.ExpectedPoints
            });
            await LuuThayDoiAsync(token);
            return session.EndedAt != null;
        }, token);

    private static AiInterviewTurn KiemTraCauTraLoi(AiInterviewSession? session, AiInterviewAnswerInput input)
    {
        if (session == null) throw new AiInterviewException("Không tìm thấy phiên luyện tập.", 404);
        if (session.EndedAt != null) throw new AiInterviewException("Phiên luyện tập đã kết thúc. Bạn tải lại trang để xem kết quả.", 409);
        var turn = session.Turns.SingleOrDefault(t => t.Number == input.Number);
        if (turn == null || turn.AnsweredAt != null || session.Turns.Max(t => t.Number) != input.Number)
            throw new AiInterviewException("Câu hỏi đã thay đổi. Bạn tải lại trang để tiếp tục.", 409);
        if (input.Answer.Trim().Length is < 2 or > 4000)
            throw new AiInterviewException("Câu trả lời cần từ 2 đến 4.000 ký tự.", 400);
        return turn;
    }

    public async Task KetThucAsync(Guid id, string userId, CancellationToken token)
    {
        var session = await db.AiInterviewSessions.SingleOrDefaultAsync(s => s.Id == id && s.CandidateId == userId, token);
        if (session == null) throw new AiInterviewException("Không tìm thấy phiên luyện tập.", 404);
        if (session.EndedAt != null) return;
        session.EndedAt = DateTime.UtcNow; session.Version = Guid.NewGuid();
        await LuuThayDoiAsync(token);
    }

    public async Task<AiInterviewReportViewModel?> LayBaoCaoAsync(Guid id, string userId, CancellationToken token)
    {
        var session = await LayPhienAsync(id, userId, token);
        if (session == null) return null;
        var turns = session.Turns.Where(t => t.AnsweredAt != null).OrderBy(t => t.Number)
            .Select(t => new AiInterviewReportTurn(t.Number, t.Question, t.Answer!, t.Feedback!, t.Improvement!, t.ExpectedPoints, t.Score!.Value)).ToList();
        return new(session.Id, session.JobTitle, session.CategoryName, session.QuestionCount,
            session.CreatedAt, session.EndedAt, turns, turns.Count == 0 ? null : (int)Math.Round(turns.Average(t => t.Score)));
    }

    private async Task LuuThayDoiAsync(CancellationToken token)
    {
        try { await db.SaveChangesAsync(token); }
        catch (DbUpdateException) { throw new AiInterviewException("Phiên đã được cập nhật ở trang khác. Bạn tải lại trang nhé.", 409); }
    }
}

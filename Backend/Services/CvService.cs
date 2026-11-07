using System.Text.Json;
using JobPortal.Data;
using JobPortal.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace JobPortal.Services;

public sealed class CvService(ApplicationDbContext database, PrivateFileStorage files, IMemoryCache cache)
{
    public IQueryable<CV> Documents => database.CVs
        .Include(c => c.Experiences).Include(c => c.Educations).Include(c => c.Skills)
        .Include(c => c.Languages).Include(c => c.References).AsSplitQuery();

    public static string DownloadUrl(int id) => "/CV/Download/" + id;

    public async Task<Candidate> GetProfileAsync(string userId, CancellationToken token)
    {
        var candidate = await database.Candidates.Include(c => c.DefaultCV).SingleOrDefaultAsync(c => c.UserId == userId, token);
        if (candidate != null) return candidate;
        candidate = new Candidate { UserId = userId };
        database.Candidates.Add(candidate);
        return candidate;
    }

    public async Task<EditCVViewModel?> GetEditorAsync(string userId, int templateId, CancellationToken token)
    {
        if (templateId is not (1 or 2) || !await database.CVTemplates.AnyAsync(t => t.Id == templateId, token)) return null;
        var profile = await database.Candidates.AsNoTracking().SingleOrDefaultAsync(c => c.UserId == userId, token);
        var current = profile?.DefaultCVId is int id ? await Documents.AsNoTracking().SingleOrDefaultAsync(c => c.Id == id && c.CandidateId == userId && !c.IsSnapshot, token) : null;
        if (current?.TemplateId != null)
        {
            var result = ToEditor(current);
            result.TemplateId = templateId;
            return result;
        }
        var email = await database.Users.Where(u => u.Id == userId).Select(u => u.Email).SingleAsync(token);
        return new EditCVViewModel
        {
            TemplateId = templateId, FullName = profile?.FullName, Email = email,
            Phone = profile?.Phone, Address = profile?.Address, AvatarPath = profile?.AvatarUrl
        };
    }

    public async Task<CV> SaveBuilderAsync(string userId, EditCVViewModel input, IFormFile pdf, IFormFile? avatar, CancellationToken token)
    {
        if (input.TemplateId is not (1 or 2) || !await database.CVTemplates.AnyAsync(t => t.Id == input.TemplateId, token))
            throw new InvalidDataException("Mẫu CV không tồn tại.");
        CV document;
        if (input.Id > 0)
            document = await Documents.SingleOrDefaultAsync(c => c.Id == input.Id && c.CandidateId == userId && !c.IsSnapshot, token)
                ?? throw new UnauthorizedAccessException("Không có quyền chỉnh sửa CV này.");
        else { document = new CV { CandidateId = userId }; database.CVs.Add(document); }

        string? newPdf = null, newAvatar = null;
        int? previousDefault = null;
        var oldPdf = document.FileName;
        var oldAvatar = document.AvatarPath;
        try
        {
            newPdf = await files.SavePdfAsync(pdf, token);
            if (avatar != null) newAvatar = await files.SaveAvatarAsync(avatar, token);
            document.FileName = newPdf;
            document.TemplateId = input.TemplateId;
            document.FullName = input.FullName?.Trim(); document.JobTitle = input.JobTitle?.Trim();
            document.Title = input.JobTitle?.Trim(); document.Email = input.Email?.Trim();
            document.Phone = input.Phone?.Trim(); document.Address = input.Address?.Trim();
            document.Website = input.Website; document.Summary = input.Summary;
            document.Interests = input.Interests; document.Activities = input.Activities; document.Certifications = input.Certifications;
            document.ThemeColor = input.ThemeColor; document.HeaderColor = input.HeaderColor; document.BackgroundColor = input.BackgroundColor;
            // Client-supplied avatar paths and AI scores are never trusted.
            var profile = await GetProfileAsync(userId, token);
            previousDefault = profile.DefaultCVId;
            document.AvatarPath = newAvatar ?? document.AvatarPath ?? profile.AvatarUrl;
            document.UploadDate = DateTime.UtcNow;
            ReplaceSections(document, input);
            if (cache.TryGetValue(GeminiCvReviewService.CacheKey(userId, input), out AiReviewResult? result) && result != null)
            {
                document.AiScore = result.Score; document.AiFeedback = JsonSerializer.Serialize(result.Suggestions);
            }
            else { document.AiScore = null; document.AiFeedback = null; }
            profile.DefaultCV = document;
            await database.SaveChangesAsync(token);
        }
        catch
        {
            files.DeleteNewPdf(newPdf); files.DeleteAvatar(newAvatar);
            throw;
        }
        await DeleteUnreferencedAsync(oldPdf, oldAvatar, token);
        if (previousDefault is int previous && previous != document.Id)
            await RemoveReplacedDraftAsync(previous, token);
        return document;
    }

    public CV CreateUploaded(string userId, string fileName, Candidate profile, string? email) => new()
    {
        CandidateId = userId, FileName = fileName, Title = "CV tải lên", FullName = profile.FullName,
        Email = email, Phone = profile.Phone, Address = profile.Address, AvatarPath = profile.AvatarUrl,
        UploadDate = DateTime.UtcNow
    };

    public async Task<CV?> CreateSnapshotAsync(string userId, int id, CancellationToken token)
    {
        var original = await Documents.AsNoTracking().SingleOrDefaultAsync(c => c.Id == id && c.CandidateId == userId && !c.IsSnapshot, token);
        if (original == null || files.GetPdfPath(original.FileName) == null) return null;
        var snapshot = new CV
        {
            CandidateId = original.CandidateId, FileName = original.FileName, Title = original.Title,
            TemplateId = original.TemplateId, IsSnapshot = true, FullName = original.FullName, JobTitle = original.JobTitle,
            Email = original.Email, Phone = original.Phone, Address = original.Address, Website = original.Website,
            Summary = original.Summary, AvatarPath = original.AvatarPath, Interests = original.Interests,
            Activities = original.Activities, Certifications = original.Certifications,
            ThemeColor = original.ThemeColor, HeaderColor = original.HeaderColor, BackgroundColor = original.BackgroundColor,
            AiScore = original.AiScore, AiFeedback = original.AiFeedback, UploadDate = DateTime.UtcNow
        };
        ReplaceSections(snapshot, ToEditor(original));
        return snapshot;
    }

    public async Task DeleteUnreferencedAsync(string? pdf, string? avatar, CancellationToken token)
    {
        if (pdf != null && !await database.CVs.AnyAsync(c => c.FileName == pdf, token)) files.DeleteNewPdf(pdf);
        if (avatar != null && !await database.CVs.AnyAsync(c => c.AvatarPath == avatar, token) &&
            !await database.Candidates.AnyAsync(c => c.AvatarUrl == avatar, token)) files.DeleteAvatar(avatar);
    }

    public async Task RemoveReplacedDraftAsync(int id, CancellationToken token)
    {
        var document = await database.CVs.SingleOrDefaultAsync(c => c.Id == id && !c.IsSnapshot, token);
        if (document == null || await database.Candidates.AnyAsync(c => c.DefaultCVId == id, token) ||
            await database.Applications.AnyAsync(a => a.CVId == id, token)) return;
        var pdf = document.FileName; var avatar = document.AvatarPath;
        database.CVs.Remove(document);
        await database.SaveChangesAsync(token);
        await DeleteUnreferencedAsync(pdf, avatar, token);
    }

    private void ReplaceSections(CV document, EditCVViewModel input)
    {
        if (document.Id != 0)
        {
            database.RemoveRange(document.Experiences); database.RemoveRange(document.Educations);
            database.RemoveRange(document.Skills); database.RemoveRange(document.Languages); database.RemoveRange(document.References);
        }
        document.Experiences = input.Experiences.Select((e, i) => new CvExperience { Company = e.Company, Role = e.Role, Duration = e.Duration, Description = e.Description, SortOrder = i }).ToList();
        document.Educations = input.Educations.Select((e, i) => new CvEducation { School = e.School, Major = e.Major, Duration = e.Duration, SortOrder = i }).ToList();
        document.Skills = input.ITSKills.Select((s, i) => new CvSkill { SkillName = s.SkillName, Description = s.Description, SortOrder = i }).ToList();
        document.Languages = input.Languages.Select((l, i) => new CvLanguage { LanguageName = l.LanguageName, Level = l.Level, SortOrder = i }).ToList();
        document.References = input.References.Select((r, i) => new CvReference { Name = r.Name, Position = r.Position, Company = r.Company, Phone = r.Phone, SortOrder = i }).ToList();
    }

    public static EditCVViewModel ToEditor(CV document) => new()
    {
        Id = document.Id, TemplateId = document.TemplateId ?? 1, FullName = document.FullName, JobTitle = document.JobTitle,
        Email = document.Email, Phone = document.Phone, Address = document.Address, Website = document.Website,
        Summary = document.Summary, AvatarPath = document.AvatarPath, Interests = document.Interests,
        Activities = document.Activities, Certifications = document.Certifications,
        ThemeColor = document.ThemeColor, HeaderColor = document.HeaderColor, BackgroundColor = document.BackgroundColor,
        AiScore = document.AiScore, AiSuggestions = ParseSuggestions(document.AiFeedback),
        Experiences = document.Experiences.OrderBy(e => e.SortOrder).Select(e => new ExperienceVM { Company = e.Company, Role = e.Role, Duration = e.Duration, Description = e.Description }).ToList(),
        Educations = document.Educations.OrderBy(e => e.SortOrder).Select(e => new EducationVM { School = e.School, Major = e.Major, Duration = e.Duration }).ToList(),
        ITSKills = document.Skills.OrderBy(s => s.SortOrder).Select(s => new ITSkillVM { SkillName = s.SkillName, Description = s.Description }).ToList(),
        Languages = document.Languages.OrderBy(l => l.SortOrder).Select(l => new LanguageVM { LanguageName = l.LanguageName, Level = l.Level }).ToList(),
        References = document.References.OrderBy(r => r.SortOrder).Select(r => new ReferenceVM { Name = r.Name, Position = r.Position, Company = r.Company, Phone = r.Phone }).ToList()
    };

    private static List<string> ParseSuggestions(string? feedback)
    {
        try { return feedback == null ? [] : JsonSerializer.Deserialize<List<string>>(feedback) ?? []; }
        catch (JsonException) { return feedback == null ? [] : [feedback]; }
    }
}

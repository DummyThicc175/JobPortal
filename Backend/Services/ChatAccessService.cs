using JobPortal.Data;
using Microsoft.EntityFrameworkCore;

namespace JobPortal.Services;

public sealed class ChatAccessService(ApplicationDbContext database)
{
    public Task<bool> CanChatAsync(string userId, string partnerId, CancellationToken token = default) =>
        userId == partnerId || string.IsNullOrWhiteSpace(partnerId)
            ? Task.FromResult(false)
            : database.Applications.AnyAsync(a =>
                (a.CandidateId == userId && a.JobPost.EmployerId == partnerId) ||
                (a.CandidateId == partnerId && a.JobPost.EmployerId == userId), token);
}

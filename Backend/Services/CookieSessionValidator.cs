using System.Security.Claims;
using JobPortal.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace JobPortal.Services;

public sealed class CookieSessionValidator(ApplicationDbContext database, IMemoryCache cache, IOptions<IdentityOptions> options)
{
    public static string Key(string id) => "account-session:" + id;

    public async Task ValidateAsync(CookieValidatePrincipalContext context)
    {
        var id = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (id == null) { context.RejectPrincipal(); return; }
        var state = await cache.GetOrCreateAsync(Key(id), async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30);
            return await database.Users.AsNoTracking().Where(u => u.Id == id)
                .Select(u => new SessionState(u.SecurityStamp, u.LockoutEnd)).SingleOrDefaultAsync(context.HttpContext.RequestAborted);
        });
        var stamp = context.Principal?.FindFirstValue(options.Value.ClaimsIdentity.SecurityStampClaimType);
        if (state == null || state.LockoutEnd > DateTimeOffset.UtcNow || state.SecurityStamp != stamp)
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
        }
    }
    private sealed record SessionState(string? SecurityStamp, DateTimeOffset? LockoutEnd);
}

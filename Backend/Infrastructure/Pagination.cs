using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobPortal.Infrastructure;

public static class Pagination
{
    public const int PageSize = 20;

    public static async Task<List<T>> PhanTrangAsync<T>(this IQueryable<T> query, Controller controller, int page, CancellationToken token = default)
    {
        var total = await query.CountAsync(token);
        var pages = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));
        page = Math.Clamp(page, 1, pages);
        controller.ViewData["Page"] = page;
        controller.ViewData["Pages"] = pages;
        controller.ViewData["Total"] = total;
        return await query.Skip((page - 1) * PageSize).Take(PageSize).ToListAsync(token);
    }
}

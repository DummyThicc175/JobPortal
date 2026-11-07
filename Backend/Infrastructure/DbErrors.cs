using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace JobPortal.Infrastructure;

public static class DbErrors
{
    public static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is SqlException { Number: 2601 or 2627 } ||
        exception.GetBaseException().Message.Contains("UNIQUE constraint failed", StringComparison.OrdinalIgnoreCase);
}

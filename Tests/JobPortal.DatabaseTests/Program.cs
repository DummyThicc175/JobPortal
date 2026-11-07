using JobPortal.Data;
using JobPortal.Services;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

const string previous = "20260527005050_UpdateAi";
var checks = 0;
void KiemTra(bool value, string description)
{
    if (!value) throw new InvalidOperationException(description);
    checks++;
}
try
{
foreach (var scenario in new[] { "fresh", "legacy", "guard" })
{
    var name = "JobPortal_Verify_" + Guid.NewGuid().ToString("N");
    var connection = new SqlConnectionStringBuilder
    { DataSource = @".\SQLEXPRESS", InitialCatalog = name, IntegratedSecurity = true, TrustServerCertificate = true };
    await using var database = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connection.ConnectionString).Options);
    try
    {
        var migrator = database.GetService<IMigrator>();
        await migrator.MigrateAsync(scenario == "fresh" ? null : previous);
        if (scenario != "fresh")
        {
            await using (var sql = new SqlConnection(connection.ConnectionString))
            {
                await sql.OpenAsync(); await using var fixture = sql.CreateCommand();
                fixture.CommandText = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "LegacyFixture.sql"));
                await fixture.ExecuteNonQueryAsync();
                if (scenario == "legacy")
                {
                    fixture.CommandText = "UPDATE dbo.Applications SET Status=2 WHERE Id=(SELECT MIN(Id) FROM dbo.Applications);";
                    await fixture.ExecuteNonQueryAsync();
                }
            }
            if (scenario == "guard")
            {
                await database.Database.ExecuteSqlRawAsync("UPDATE dbo.Candidates SET Industry=N'legacy information';");
                var rejected = false;
                try { await migrator.MigrateAsync(); }
                catch (SqlException error) when (error.Number == 51000) { rejected = true; }
                KiemTra(rejected, "Migration must stop when removed fields contain legacy data");
                await using var sql = new SqlConnection(connection.ConnectionString); await sql.OpenAsync();
                await using var command = sql.CreateCommand();
                command.CommandText = "SELECT COUNT(*) FROM dbo.Candidates WHERE Industry=N'legacy information';";
                KiemTra(Convert.ToInt32(await command.ExecuteScalarAsync()) == 1, "Guard must preserve all original data");
                KiemTra((await database.Database.GetAppliedMigrationsAsync()).Count() == 26, "Failed migration must not change migration history");
                Console.WriteLine("PASS: rollback guard");
                continue;
            }
            await migrator.MigrateAsync();
        }
        KiemTra(!(await database.Database.GetPendingMigrationsAsync()).Any(), "All migrations applied");
        KiemTra(!database.Database.HasPendingModelChanges(), "Snapshot must match the runtime SQL Server model");
        KiemTra(await database.Users.CountAsync() == 3, "Default accounts preserved");
        KiemTra(await database.CVTemplates.CountAsync() == 6, "Six real templates seeded");
        KiemTra(await database.Users.AllAsync(u => u.LockoutEnabled), "Account lockout enabled");
        if (scenario == "legacy")
        {
            var room = await database.InterviewSessions.Include(s => s.Application).SingleAsync();
            KiemTra(room.Application.Status == JobPortal.Models.ApplicationStatus.Interview && room.EndedAt == null,
                "Existing interview application receives a usable room during migration");
            var duplicateRoom = new JobPortal.Models.InterviewSession { ApplicationId = room.ApplicationId };
            database.InterviewSessions.Add(duplicateRoom);
            var duplicateRejected = false;
            try { await database.SaveChangesAsync(); }
            catch (DbUpdateException error) when (JobPortal.Infrastructure.DbErrors.LaLoiTrungDuLieu(error)) { duplicateRejected = true; }
            database.Entry(duplicateRoom).State = EntityState.Detached;
            KiemTra(duplicateRejected, "SQL Server rejects two active interview rooms for one application");
            room.EndedAt = DateTime.UtcNow;
            await database.SaveChangesAsync();
            database.InterviewSessions.Add(new JobPortal.Models.InterviewSession { ApplicationId = room.ApplicationId });
            await database.SaveChangesAsync();
            KiemTra(await database.InterviewSessions.CountAsync() == 2 && await database.InterviewSessions.CountAsync(s => s.EndedAt == null) == 1,
                "Closed room history does not prevent a new active session");
            var profile = await database.Candidates.Include(c => c.DefaultCV).SingleAsync();
            KiemTra(profile.FullName == "Legacy Candidate" && profile.UserId == "d83b7113-a8f8-4035-99a7-a20dd400f6a4", "Profile is mapped by stable user ID");
            KiemTra(profile.DefaultCV?.FileName == "legacy-upload.pdf", "Legacy profile PDF preserved");
            var json = await database.CVs.Include(c => c.Experiences).Include(c => c.Educations).Include(c => c.Skills).AsSplitQuery().SingleAsync(c => c.Title == "JSON CV" && !c.IsSnapshot);
            KiemTra(json.FileName == "json-cv.pdf", "Legacy file URL converted to safe key");
            KiemTra(json.Experiences.Single().Company == "JSON Company" && json.Educations.Single().School == "JSON School" && json.Skills.Count == 2, "JSON sections normalized without losing data");
            var builder = await database.CVs.Include(c => c.Experiences).Include(c => c.Educations).Include(c => c.Skills).Include(c => c.Languages).Include(c => c.References).AsSplitQuery().SingleAsync(c => c.Title == "Builder Developer");
            KiemTra(builder.Experiences.Single().Company == "Builder Company" && builder.Educations.Single().School == "Builder School", "Old builder education and experience linked to canonical CV");
            KiemTra(builder.Skills.Single().SkillName == "C#" && builder.Languages.Single().LanguageName == "English" && builder.References.Single().Name == "Referee", "Old builder remaining sections preserved");
            var jobs = await database.JobPosts.Include(j => j.Company).Include(j => j.Category).ToListAsync();
            KiemTra(jobs.All(j => j.Company?.Name == "Legacy Company" && j.Category?.Name == "Legacy Category"), "Text company/category names resolved to foreign keys");
            var submissions = await database.Applications.Include(a => a.CV).ToListAsync();
            KiemTra(submissions.Count == 2 && submissions.All(a => a.CV?.IsSnapshot == true), "Historical submissions receive immutable snapshots");
            KiemTra(submissions.Any(a => a.CV?.FileName == "legacy-upload.pdf") && submissions.Any(a => a.CV?.FileName == "json-cv.pdf"), "Existing and fallback submission CVs preserved");
            KiemTra(await database.SavedJobs.CountAsync() == 1 && await database.ChatMessages.CountAsync() == 1, "Saved jobs and chat preserved");
            await using var sql = new SqlConnection(connection.ConnectionString); await sql.OpenAsync();
            await using var command = sql.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM sys.tables WHERE name LIKE '__CvUpgrade[_]%' OR name='UserCVs';";
            KiemTra(Convert.ToInt32(await command.ExecuteScalarAsync()) == 0, "Staging and duplicate CV tables removed after success");
            command.CommandText = "SELECT COUNT(*) FROM sys.columns WHERE object_id=OBJECT_ID('dbo.AspNetUsers') AND name IN ('FullName','BirthDate','Gender');";
            KiemTra(Convert.ToInt32(await command.ExecuteScalarAsync()) == 0, "Unmapped user columns consolidated");
        }
        Console.WriteLine("PASS: " + scenario + " SQL Server migration");
    }
    finally
    {
        if (!name.StartsWith("JobPortal_Verify_", StringComparison.Ordinal) || !Guid.TryParseExact(name[17..], "N", out _))
            throw new InvalidOperationException("Refusing to remove unexpected database");
        await database.Database.EnsureDeletedAsync();
    }
}
Console.WriteLine($"PASS: {checks} SQL Server migration assertions; only disposable JobPortal_Verify_<GUID> databases were used.");
}
catch (Exception error) { Console.Error.WriteLine(error); Environment.ExitCode = 1; }

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace JobPortal.Migrations
{
    /// <inheritdoc />
    public partial class SeedDefaultUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { "02174cfc-2dc1-460d-8521-396c0966a4f0", null, "Employer", "EMPLOYER" },
                    { "7d9b7113-a8f8-4035-99a7-a20dd400f6a3", null, "Candidate", "CANDIDATE" },
                    { "8505d10d-7730-4e57-96fd-99335279b974", null, "Admin", "ADMIN" }
                });

            migrationBuilder.InsertData(
                table: "AspNetUsers",
                columns: new[] { "Id", "AccessFailedCount", "ConcurrencyStamp", "Email", "EmailConfirmed", "LockoutEnabled", "LockoutEnd", "NormalizedEmail", "NormalizedUserName", "PasswordHash", "PhoneNumber", "PhoneNumberConfirmed", "SecurityStamp", "TwoFactorEnabled", "UserName" },
                values: new object[,]
                {
                    { "b74ddd14-6340-4840-95c2-db12554843e5", 0, "0a55a4bc-a6dd-4cba-9573-1a4b582d0005", "admin@jobhub.com", true, false, null, "ADMIN@JOBHUB.COM", "ADMIN@JOBHUB.COM", "AQAAAAIAAYagAAAAEA325iizvA61b+YzhZmnFUDipZdIhBl21Mxur4ZYt79EWk+IXUC3ljzBm6jxSvTScA==", null, false, "cfc997f9-c2b9-4a2e-8498-9ab3cfe010e4", false, "admin@jobhub.com" },
                    { "c9284cfc-2dc1-460d-8521-396c0966a4f1", 0, "c643469b-9f7d-4580-a885-c765be7f97a3", "employer@jobhub.com", true, false, null, "EMPLOYER@JOBHUB.COM", "EMPLOYER@JOBHUB.COM", "AQAAAAIAAYagAAAAEEFTG78SLv5u1QrqR/Axlac4lh7Izmh/GcQvaeMA8A098SOabXbXGsqYrkORGn9hlw==", null, false, "42670bbd-3b24-4bd0-a6c5-9bbf8c880ccb", false, "employer@jobhub.com" },
                    { "d83b7113-a8f8-4035-99a7-a20dd400f6a4", 0, "4f3cdc1f-eb0d-49bc-a4a3-a083804f042f", "candidate@jobhub.com", true, false, null, "CANDIDATE@JOBHUB.COM", "CANDIDATE@JOBHUB.COM", "AQAAAAIAAYagAAAAEIM4X6cmV/czWwax/m3keBj0ozuVNeixbSLBg8NlSOEqln9HuG7Fgh8uu0xLNekNmQ==", null, false, "dd293037-743d-4a25-87f6-327a1d0bcda1", false, "candidate@jobhub.com" }
                });

            migrationBuilder.InsertData(
                table: "AspNetUserRoles",
                columns: new[] { "RoleId", "UserId" },
                values: new object[,]
                {
                    { "8505d10d-7730-4e57-96fd-99335279b974", "b74ddd14-6340-4840-95c2-db12554843e5" },
                    { "02174cfc-2dc1-460d-8521-396c0966a4f0", "c9284cfc-2dc1-460d-8521-396c0966a4f1" },
                    { "7d9b7113-a8f8-4035-99a7-a20dd400f6a3", "d83b7113-a8f8-4035-99a7-a20dd400f6a4" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetUserRoles",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { "8505d10d-7730-4e57-96fd-99335279b974", "b74ddd14-6340-4840-95c2-db12554843e5" });

            migrationBuilder.DeleteData(
                table: "AspNetUserRoles",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { "02174cfc-2dc1-460d-8521-396c0966a4f0", "c9284cfc-2dc1-460d-8521-396c0966a4f1" });

            migrationBuilder.DeleteData(
                table: "AspNetUserRoles",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { "7d9b7113-a8f8-4035-99a7-a20dd400f6a3", "d83b7113-a8f8-4035-99a7-a20dd400f6a4" });

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "02174cfc-2dc1-460d-8521-396c0966a4f0");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "7d9b7113-a8f8-4035-99a7-a20dd400f6a3");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "8505d10d-7730-4e57-96fd-99335279b974");

            migrationBuilder.DeleteData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "b74ddd14-6340-4840-95c2-db12554843e5");

            migrationBuilder.DeleteData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "c9284cfc-2dc1-460d-8521-396c0966a4f1");

            migrationBuilder.DeleteData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "d83b7113-a8f8-4035-99a7-a20dd400f6a4");
        }
    }
}

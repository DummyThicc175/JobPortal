using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobPortal.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCVTableForBuilder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Activities",
                table: "CVs");

            migrationBuilder.DropColumn(
                name: "Certifications",
                table: "CVs");

            migrationBuilder.DropColumn(
                name: "Interests",
                table: "CVs");

            migrationBuilder.DropColumn(
                name: "LanguagesJson",
                table: "CVs");

            migrationBuilder.DropColumn(
                name: "ReferencesJson",
                table: "CVs");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "b74ddd14-6340-4840-95c2-db12554843e5",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "3a5c18dd-42ce-45ca-bb8a-b88124469b8a", "AQAAAAIAAYagAAAAEJQ3nYFA/fDLHW4997Tuk+cpUivCKFV8CsmLkTK9kMJdliWLLdb8tHkkYn3pRIQmcw==", "cf203d8a-253b-4109-8e62-dea9def191c1" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "c9284cfc-2dc1-460d-8521-396c0966a4f1",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "2f1b96ec-a6b1-4708-b369-eb483743b897", "AQAAAAIAAYagAAAAEJVeJjSpMYwrHYyuOqyR0YtK+4vhXMR+F49jkAfbf9jwdn23PcoajzVRnHE2bFI+Pw==", "4ebca9a4-f92a-4ecc-9036-0e19ddf26b32" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "d83b7113-a8f8-4035-99a7-a20dd400f6a4",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "82d4ebf3-44a4-452a-8211-e0dd29b403c4", "AQAAAAIAAYagAAAAEHg9/eCWjoAdRqQGxlgA+pRzMWCExK2C66GURxX7cN4zB3/Rrb5SYPCTiFoevaUfIA==", "23200f2d-0048-4631-b7f6-15b72b175361" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Activities",
                table: "CVs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Certifications",
                table: "CVs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Interests",
                table: "CVs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LanguagesJson",
                table: "CVs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReferencesJson",
                table: "CVs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "b74ddd14-6340-4840-95c2-db12554843e5",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "4c6e527c-b8f5-4e97-a763-93634e4145f3", "AQAAAAIAAYagAAAAEEXOI35mJ5bMXsYFTDIEg+QaE1kZsD+d7g6cpOXECsol1OsMw/qDL7+lYgmjEBZ/KA==", "977ce9d1-8edb-49ef-913e-987f1bc2135d" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "c9284cfc-2dc1-460d-8521-396c0966a4f1",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "e7bf468a-045b-48da-91c7-42ebdc275ac3", "AQAAAAIAAYagAAAAEC1ymDMGb07Q50LKphw/QDdyEqBHlzClEsa6bhzHulkrn6oGPX+2QyoQQwBQ+h7tGw==", "88befe9e-96ba-4685-bd65-6da7d329599a" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "d83b7113-a8f8-4035-99a7-a20dd400f6a4",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "94bcd8bf-0d7b-4d8e-84e5-2435a41a4fa9", "AQAAAAIAAYagAAAAEK5uHOG4slLiKOZ09HtQXMwM3poDuc7fpt0Yvr/k1/TBytzHK3+Xbqm4I1yg/FR/Yg==", "2b93fa4c-cf64-4677-a823-f5a2c572ef4c" });
        }
    }
}

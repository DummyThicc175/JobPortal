using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobPortal.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCVForBuilderAndAI : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "FileName",
                table: "CVs",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "Activities",
                table: "CVs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "CVs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AiFeedback",
                table: "CVs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AiScore",
                table: "CVs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Certifications",
                table: "CVs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EducationsJson",
                table: "CVs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "CVs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExperiencesJson",
                table: "CVs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FullName",
                table: "CVs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Interests",
                table: "CVs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "JobTitle",
                table: "CVs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LanguagesJson",
                table: "CVs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "CVs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReferencesJson",
                table: "CVs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SkillsJson",
                table: "CVs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TemplateId",
                table: "CVs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Website",
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Activities",
                table: "CVs");

            migrationBuilder.DropColumn(
                name: "Address",
                table: "CVs");

            migrationBuilder.DropColumn(
                name: "AiFeedback",
                table: "CVs");

            migrationBuilder.DropColumn(
                name: "AiScore",
                table: "CVs");

            migrationBuilder.DropColumn(
                name: "Certifications",
                table: "CVs");

            migrationBuilder.DropColumn(
                name: "EducationsJson",
                table: "CVs");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "CVs");

            migrationBuilder.DropColumn(
                name: "ExperiencesJson",
                table: "CVs");

            migrationBuilder.DropColumn(
                name: "FullName",
                table: "CVs");

            migrationBuilder.DropColumn(
                name: "Interests",
                table: "CVs");

            migrationBuilder.DropColumn(
                name: "JobTitle",
                table: "CVs");

            migrationBuilder.DropColumn(
                name: "LanguagesJson",
                table: "CVs");

            migrationBuilder.DropColumn(
                name: "Phone",
                table: "CVs");

            migrationBuilder.DropColumn(
                name: "ReferencesJson",
                table: "CVs");

            migrationBuilder.DropColumn(
                name: "SkillsJson",
                table: "CVs");

            migrationBuilder.DropColumn(
                name: "TemplateId",
                table: "CVs");

            migrationBuilder.DropColumn(
                name: "Website",
                table: "CVs");

            migrationBuilder.AlterColumn<string>(
                name: "FileName",
                table: "CVs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "b74ddd14-6340-4840-95c2-db12554843e5",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "6725557d-a26a-43f9-abdb-d8c1126c3cca", "AQAAAAIAAYagAAAAENaYNc2x4zDQalh5PdHFT1oK4noaYDC3afOd2JOyjfYuMkd2EvfqLUsvBdO3tmiDlA==", "05c30bac-473e-4493-8073-149ad45bbe81" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "c9284cfc-2dc1-460d-8521-396c0966a4f1",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "0b208e86-ea3a-4b41-977d-1217512e7702", "AQAAAAIAAYagAAAAEIrZkiGg8/dBbFEgWNc4htRuziMmgcJDNBryiUX1JKwrw3jfAyWHeTIumqn5ujrDLQ==", "0ef165b7-6bb2-400a-8e83-9db8df82af1c" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "d83b7113-a8f8-4035-99a7-a20dd400f6a4",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "5dd0a7f6-5e83-412c-9c13-44e7071ddf1f", "AQAAAAIAAYagAAAAEDsSRomObWB1tPTfguo1vwNJyJAWSOX7KUWoLx80e/XRoArhU9SLNqK/pggRRwDbXg==", "bbbce6c5-d832-4f40-a203-ea01275a02b0" });
        }
    }
}

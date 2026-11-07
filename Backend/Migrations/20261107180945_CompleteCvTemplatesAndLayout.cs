using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace JobPortal.Backend.Migrations
{
    /// <inheritdoc />
    public partial class CompleteCvTemplatesAndLayout : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LayoutJson",
                table: "CVs",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "CVTemplates",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Name", "Thumbnail" },
                values: new object[] { "Chuyên nghiệp", "images/templates/cv-1.svg" });

            migrationBuilder.UpdateData(
                table: "CVTemplates",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Name", "Thumbnail" },
                values: new object[] { "Thanh lịch", "images/templates/cv-2.svg" });

            migrationBuilder.InsertData(
                table: "CVTemplates",
                columns: new[] { "Id", "Category", "Name", "TemplateViewPath", "Thumbnail" },
                values: new object[,]
                {
                    { 3, "Creative", "Công nghệ", "Template3", "images/templates/cv-3.svg" },
                    { 4, "Popular", "Tối giản", "Template4", "images/templates/cv-4.svg" },
                    { 5, "Creative", "Hiện đại", "Template5", "images/templates/cv-5.svg" },
                    { 6, "Popular", "Cổ điển", "Template6", "images/templates/cv-6.svg" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "CVTemplates",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "CVTemplates",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "CVTemplates",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "CVTemplates",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DropColumn(
                name: "LayoutJson",
                table: "CVs");

            migrationBuilder.UpdateData(
                table: "CVTemplates",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Name", "Thumbnail" },
                values: new object[] { "Mẫu Thiết Kế Truyền Thống", "images/templates/t1.jpg" });

            migrationBuilder.UpdateData(
                table: "CVTemplates",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Name", "Thumbnail" },
                values: new object[] { "Mẫu Công Nghệ Hiện Đại", "images/templates/t2.jpg" });
        }
    }
}

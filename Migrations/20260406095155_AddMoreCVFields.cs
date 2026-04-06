using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobPortal.Migrations
{
    /// <inheritdoc />
    public partial class AddMoreCVFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Activities",
                table: "UserCVs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Certifications",
                table: "UserCVs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Interests",
                table: "UserCVs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ITSkillVM",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SkillName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EditCVViewModelId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ITSkillVM", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ITSkillVM_UserCVs_EditCVViewModelId",
                        column: x => x.EditCVViewModelId,
                        principalTable: "UserCVs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LanguageVM",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LanguageName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Level = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EditCVViewModelId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LanguageVM", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LanguageVM_UserCVs_EditCVViewModelId",
                        column: x => x.EditCVViewModelId,
                        principalTable: "UserCVs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReferenceVM",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Position = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Company = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EditCVViewModelId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReferenceVM", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReferenceVM_UserCVs_EditCVViewModelId",
                        column: x => x.EditCVViewModelId,
                        principalTable: "UserCVs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ITSkillVM_EditCVViewModelId",
                table: "ITSkillVM",
                column: "EditCVViewModelId");

            migrationBuilder.CreateIndex(
                name: "IX_LanguageVM_EditCVViewModelId",
                table: "LanguageVM",
                column: "EditCVViewModelId");

            migrationBuilder.CreateIndex(
                name: "IX_ReferenceVM_EditCVViewModelId",
                table: "ReferenceVM",
                column: "EditCVViewModelId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ITSkillVM");

            migrationBuilder.DropTable(
                name: "LanguageVM");

            migrationBuilder.DropTable(
                name: "ReferenceVM");

            migrationBuilder.DropColumn(
                name: "Activities",
                table: "UserCVs");

            migrationBuilder.DropColumn(
                name: "Certifications",
                table: "UserCVs");

            migrationBuilder.DropColumn(
                name: "Interests",
                table: "UserCVs");
        }
    }
}

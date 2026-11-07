using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobPortal.Migrations
{
    /// <inheritdoc />
    public partial class InitialCVTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ITSkillVM_UserCVs_EditCVViewModelId",
                table: "ITSkillVM");

            migrationBuilder.DropForeignKey(
                name: "FK_LanguageVM_UserCVs_EditCVViewModelId",
                table: "LanguageVM");

            migrationBuilder.DropForeignKey(
                name: "FK_ReferenceVM_UserCVs_EditCVViewModelId",
                table: "ReferenceVM");

            migrationBuilder.DropForeignKey(
                name: "FK_UserEducations_UserCVs_EditCVViewModelId",
                table: "UserEducations");

            migrationBuilder.DropForeignKey(
                name: "FK_UserExperiences_UserCVs_EditCVViewModelId",
                table: "UserExperiences");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserExperiences",
                table: "UserExperiences");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserEducations",
                table: "UserEducations");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ReferenceVM",
                table: "ReferenceVM");

            migrationBuilder.DropPrimaryKey(
                name: "PK_LanguageVM",
                table: "LanguageVM");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ITSkillVM",
                table: "ITSkillVM");

            migrationBuilder.RenameTable(
                name: "UserExperiences",
                newName: "Experiences");

            migrationBuilder.RenameTable(
                name: "UserEducations",
                newName: "Educations");

            migrationBuilder.RenameTable(
                name: "ReferenceVM",
                newName: "References");

            migrationBuilder.RenameTable(
                name: "LanguageVM",
                newName: "Languages");

            migrationBuilder.RenameTable(
                name: "ITSkillVM",
                newName: "ITSKills");

            migrationBuilder.RenameIndex(
                name: "IX_UserExperiences_EditCVViewModelId",
                table: "Experiences",
                newName: "IX_Experiences_EditCVViewModelId");

            migrationBuilder.RenameIndex(
                name: "IX_UserEducations_EditCVViewModelId",
                table: "Educations",
                newName: "IX_Educations_EditCVViewModelId");

            migrationBuilder.RenameIndex(
                name: "IX_ReferenceVM_EditCVViewModelId",
                table: "References",
                newName: "IX_References_EditCVViewModelId");

            migrationBuilder.RenameIndex(
                name: "IX_LanguageVM_EditCVViewModelId",
                table: "Languages",
                newName: "IX_Languages_EditCVViewModelId");

            migrationBuilder.RenameIndex(
                name: "IX_ITSkillVM_EditCVViewModelId",
                table: "ITSKills",
                newName: "IX_ITSKills_EditCVViewModelId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Experiences",
                table: "Experiences",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Educations",
                table: "Educations",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_References",
                table: "References",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Languages",
                table: "Languages",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ITSKills",
                table: "ITSKills",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Educations_UserCVs_EditCVViewModelId",
                table: "Educations",
                column: "EditCVViewModelId",
                principalTable: "UserCVs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Experiences_UserCVs_EditCVViewModelId",
                table: "Experiences",
                column: "EditCVViewModelId",
                principalTable: "UserCVs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ITSKills_UserCVs_EditCVViewModelId",
                table: "ITSKills",
                column: "EditCVViewModelId",
                principalTable: "UserCVs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Languages_UserCVs_EditCVViewModelId",
                table: "Languages",
                column: "EditCVViewModelId",
                principalTable: "UserCVs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_References_UserCVs_EditCVViewModelId",
                table: "References",
                column: "EditCVViewModelId",
                principalTable: "UserCVs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Educations_UserCVs_EditCVViewModelId",
                table: "Educations");

            migrationBuilder.DropForeignKey(
                name: "FK_Experiences_UserCVs_EditCVViewModelId",
                table: "Experiences");

            migrationBuilder.DropForeignKey(
                name: "FK_ITSKills_UserCVs_EditCVViewModelId",
                table: "ITSKills");

            migrationBuilder.DropForeignKey(
                name: "FK_Languages_UserCVs_EditCVViewModelId",
                table: "Languages");

            migrationBuilder.DropForeignKey(
                name: "FK_References_UserCVs_EditCVViewModelId",
                table: "References");

            migrationBuilder.DropPrimaryKey(
                name: "PK_References",
                table: "References");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Languages",
                table: "Languages");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ITSKills",
                table: "ITSKills");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Experiences",
                table: "Experiences");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Educations",
                table: "Educations");

            migrationBuilder.RenameTable(
                name: "References",
                newName: "ReferenceVM");

            migrationBuilder.RenameTable(
                name: "Languages",
                newName: "LanguageVM");

            migrationBuilder.RenameTable(
                name: "ITSKills",
                newName: "ITSkillVM");

            migrationBuilder.RenameTable(
                name: "Experiences",
                newName: "UserExperiences");

            migrationBuilder.RenameTable(
                name: "Educations",
                newName: "UserEducations");

            migrationBuilder.RenameIndex(
                name: "IX_References_EditCVViewModelId",
                table: "ReferenceVM",
                newName: "IX_ReferenceVM_EditCVViewModelId");

            migrationBuilder.RenameIndex(
                name: "IX_Languages_EditCVViewModelId",
                table: "LanguageVM",
                newName: "IX_LanguageVM_EditCVViewModelId");

            migrationBuilder.RenameIndex(
                name: "IX_ITSKills_EditCVViewModelId",
                table: "ITSkillVM",
                newName: "IX_ITSkillVM_EditCVViewModelId");

            migrationBuilder.RenameIndex(
                name: "IX_Experiences_EditCVViewModelId",
                table: "UserExperiences",
                newName: "IX_UserExperiences_EditCVViewModelId");

            migrationBuilder.RenameIndex(
                name: "IX_Educations_EditCVViewModelId",
                table: "UserEducations",
                newName: "IX_UserEducations_EditCVViewModelId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ReferenceVM",
                table: "ReferenceVM",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_LanguageVM",
                table: "LanguageVM",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ITSkillVM",
                table: "ITSkillVM",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserExperiences",
                table: "UserExperiences",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserEducations",
                table: "UserEducations",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ITSkillVM_UserCVs_EditCVViewModelId",
                table: "ITSkillVM",
                column: "EditCVViewModelId",
                principalTable: "UserCVs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LanguageVM_UserCVs_EditCVViewModelId",
                table: "LanguageVM",
                column: "EditCVViewModelId",
                principalTable: "UserCVs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ReferenceVM_UserCVs_EditCVViewModelId",
                table: "ReferenceVM",
                column: "EditCVViewModelId",
                principalTable: "UserCVs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserEducations_UserCVs_EditCVViewModelId",
                table: "UserEducations",
                column: "EditCVViewModelId",
                principalTable: "UserCVs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserExperiences_UserCVs_EditCVViewModelId",
                table: "UserExperiences",
                column: "EditCVViewModelId",
                principalTable: "UserCVs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

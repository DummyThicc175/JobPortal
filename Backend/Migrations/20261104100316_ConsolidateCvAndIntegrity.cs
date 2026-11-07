using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobPortal.Migrations
{
    /// <inheritdoc />
    public partial class ConsolidateCvAndIntegrity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(ReadScript("Before"));
            migrationBuilder.DropForeignKey(
                name: "FK_Applications_CVs_CVId",
                table: "Applications");

            migrationBuilder.DropForeignKey(
                name: "FK_Applications_JobPosts_JobPostId",
                table: "Applications");

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

            migrationBuilder.DropForeignKey(
                name: "FK_SavedJobs_JobPosts_JobPostId",
                table: "SavedJobs");

            migrationBuilder.DropTable(
                name: "UserCVs");

            migrationBuilder.DropIndex(
                name: "IX_References_EditCVViewModelId",
                table: "References");

            migrationBuilder.DropIndex(
                name: "IX_Languages_EditCVViewModelId",
                table: "Languages");

            migrationBuilder.DropIndex(
                name: "IX_ITSKills_EditCVViewModelId",
                table: "ITSKills");

            migrationBuilder.DropIndex(
                name: "IX_Experiences_EditCVViewModelId",
                table: "Experiences");

            migrationBuilder.DropIndex(
                name: "IX_Educations_EditCVViewModelId",
                table: "Educations");

            migrationBuilder.DropColumn(
                name: "CategoryName",
                table: "JobPosts");

            migrationBuilder.DropColumn(
                name: "CompanyName",
                table: "JobPosts");

            migrationBuilder.DropColumn(
                name: "EducationsJson",
                table: "CVs");

            migrationBuilder.DropColumn(
                name: "ExperiencesJson",
                table: "CVs");

            migrationBuilder.DropColumn(
                name: "SkillsJson",
                table: "CVs");

            migrationBuilder.DropColumn(
                name: "CVUrl",
                table: "Candidates");

            migrationBuilder.DropColumn(
                name: "DesiredLocation",
                table: "Candidates");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Candidates");

            migrationBuilder.DropColumn(
                name: "ExpectedSalary",
                table: "Candidates");

            migrationBuilder.DropColumn(
                name: "Industry",
                table: "Candidates");

            migrationBuilder.DropColumn(
                name: "MaritalStatus",
                table: "Candidates");

            migrationBuilder.RenameColumn(
                name: "EditCVViewModelId",
                table: "References",
                newName: "SortOrder");

            migrationBuilder.RenameColumn(
                name: "EditCVViewModelId",
                table: "Languages",
                newName: "SortOrder");

            migrationBuilder.RenameColumn(
                name: "EditCVViewModelId",
                table: "ITSKills",
                newName: "SortOrder");

            migrationBuilder.RenameColumn(
                name: "EditCVViewModelId",
                table: "Experiences",
                newName: "SortOrder");

            migrationBuilder.RenameColumn(
                name: "EditCVViewModelId",
                table: "Educations",
                newName: "SortOrder");

            migrationBuilder.AlterColumn<string>(
                name: "CandidateId",
                table: "SavedJobs",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Position",
                table: "References",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Phone",
                table: "References",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "References",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Company",
                table: "References",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CVId",
                table: "References",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Locations",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Level",
                table: "Languages",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "LanguageName",
                table: "Languages",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CVId",
                table: "Languages",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "JobPosts",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "SalaryRange",
                table: "JobPosts",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "EmployerId",
                table: "JobPosts",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SkillName",
                table: "ITSKills",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "ITSKills",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CVId",
                table: "ITSKills",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "Role",
                table: "Experiences",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Duration",
                table: "Experiences",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Experiences",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Company",
                table: "Experiences",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CVId",
                table: "Experiences",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "School",
                table: "Educations",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Major",
                table: "Educations",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Duration",
                table: "Educations",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CVId",
                table: "Educations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "Thumbnail",
                table: "CVTemplates",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TemplateViewPath",
                table: "CVTemplates",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "CVTemplates",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Category",
                table: "CVTemplates",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Website",
                table: "CVs",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "CVs",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Summary",
                table: "CVs",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Phone",
                table: "CVs",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "JobTitle",
                table: "CVs",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FullName",
                table: "CVs",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FileName",
                table: "CVs",
                type: "nvarchar(260)",
                maxLength: 260,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "CVs",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CandidateId",
                table: "CVs",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Address",
                table: "CVs",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Activities",
                table: "CVs",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AvatarPath",
                table: "CVs",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BackgroundColor",
                table: "CVs",
                type: "nvarchar(7)",
                maxLength: 7,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Certifications",
                table: "CVs",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HeaderColor",
                table: "CVs",
                type: "nvarchar(7)",
                maxLength: 7,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Interests",
                table: "CVs",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSnapshot",
                table: "CVs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ThemeColor",
                table: "CVs",
                type: "nvarchar(7)",
                maxLength: 7,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Companies",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "EmployerId",
                table: "Companies",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "SenderId",
                table: "ChatMessages",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "ReceiverId",
                table: "ChatMessages",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Content",
                table: "ChatMessages",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Categories",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Phone",
                table: "Candidates",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Gender",
                table: "Candidates",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FullName",
                table: "Candidates",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AvatarUrl",
                table: "Candidates",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Address",
                table: "Candidates",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DefaultCVId",
                table: "Candidates",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "Candidates",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "CandidateId",
                table: "Applications",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.UpdateData(
                table: "CVTemplates",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Category", "Name", "TemplateViewPath" },
                values: new object[] { "Popular", "Mẫu Thiết Kế Truyền Thống", "Template1" });

            migrationBuilder.InsertData(
                table: "CVTemplates",
                columns: new[] { "Id", "Category", "Name", "TemplateViewPath", "Thumbnail" },
                values: new object[] { 2, "Creative", "Mẫu Công Nghệ Hiện Đại", "Template2", "images/templates/t2.jpg" });

            migrationBuilder.CreateIndex(
                name: "IX_SavedJobs_CandidateId_JobPostId",
                table: "SavedJobs",
                columns: new[] { "CandidateId", "JobPostId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_References_CVId",
                table: "References",
                column: "CVId");

            migrationBuilder.CreateIndex(
                name: "IX_Locations_Name",
                table: "Locations",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Languages_CVId",
                table: "Languages",
                column: "CVId");

            migrationBuilder.CreateIndex(
                name: "IX_JobPosts_EmployerId_CreatedAt",
                table: "JobPosts",
                columns: new[] { "EmployerId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_JobPosts_IsApproved_CreatedAt",
                table: "JobPosts",
                columns: new[] { "IsApproved", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ITSKills_CVId",
                table: "ITSKills",
                column: "CVId");

            migrationBuilder.CreateIndex(
                name: "IX_Experiences_CVId",
                table: "Experiences",
                column: "CVId");

            migrationBuilder.CreateIndex(
                name: "IX_Educations_CVId",
                table: "Educations",
                column: "CVId");

            migrationBuilder.CreateIndex(
                name: "IX_CVs_CandidateId_IsSnapshot_UploadDate",
                table: "CVs",
                columns: new[] { "CandidateId", "IsSnapshot", "UploadDate" });

            migrationBuilder.CreateIndex(
                name: "IX_CVs_FileName",
                table: "CVs",
                column: "FileName");

            migrationBuilder.CreateIndex(
                name: "IX_CVs_TemplateId",
                table: "CVs",
                column: "TemplateId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CVs_AiScore",
                table: "CVs",
                sql: "[AiScore] IS NULL OR ([AiScore] >= 0 AND [AiScore] <= 100)");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_EmployerId_Name",
                table: "Companies",
                columns: new[] { "EmployerId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_ReceiverId_IsRead",
                table: "ChatMessages",
                columns: new[] { "ReceiverId", "IsRead" });

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_SenderId_ReceiverId_Id",
                table: "ChatMessages",
                columns: new[] { "SenderId", "ReceiverId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Name",
                table: "Categories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Candidates_DefaultCVId",
                table: "Candidates",
                column: "DefaultCVId");

            migrationBuilder.CreateIndex(
                name: "IX_Candidates_UserId",
                table: "Candidates",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Applications_CandidateId_JobPostId",
                table: "Applications",
                columns: new[] { "CandidateId", "JobPostId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Applications_AspNetUsers_CandidateId",
                table: "Applications",
                column: "CandidateId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Applications_CVs_CVId",
                table: "Applications",
                column: "CVId",
                principalTable: "CVs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Applications_JobPosts_JobPostId",
                table: "Applications",
                column: "JobPostId",
                principalTable: "JobPosts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Candidates_AspNetUsers_UserId",
                table: "Candidates",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Candidates_CVs_DefaultCVId",
                table: "Candidates",
                column: "DefaultCVId",
                principalTable: "CVs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ChatMessages_AspNetUsers_ReceiverId",
                table: "ChatMessages",
                column: "ReceiverId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ChatMessages_AspNetUsers_SenderId",
                table: "ChatMessages",
                column: "SenderId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Companies_AspNetUsers_EmployerId",
                table: "Companies",
                column: "EmployerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CVs_AspNetUsers_CandidateId",
                table: "CVs",
                column: "CandidateId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CVs_CVTemplates_TemplateId",
                table: "CVs",
                column: "TemplateId",
                principalTable: "CVTemplates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Educations_CVs_CVId",
                table: "Educations",
                column: "CVId",
                principalTable: "CVs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Experiences_CVs_CVId",
                table: "Experiences",
                column: "CVId",
                principalTable: "CVs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ITSKills_CVs_CVId",
                table: "ITSKills",
                column: "CVId",
                principalTable: "CVs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_JobPosts_AspNetUsers_EmployerId",
                table: "JobPosts",
                column: "EmployerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Languages_CVs_CVId",
                table: "Languages",
                column: "CVId",
                principalTable: "CVs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_References_CVs_CVId",
                table: "References",
                column: "CVId",
                principalTable: "CVs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SavedJobs_AspNetUsers_CandidateId",
                table: "SavedJobs",
                column: "CandidateId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SavedJobs_JobPosts_JobPostId",
                table: "SavedJobs",
                column: "JobPostId",
                principalTable: "JobPosts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
            migrationBuilder.Sql(ReadScript("After"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new InvalidOperationException("This data consolidation cannot be downgraded safely. Restore the pre-upgrade SQL Server backup instead.");
        }

        private static string ReadScript(string phase)
        {
            using var stream = typeof(ConsolidateCvAndIntegrity).Assembly.GetManifestResourceStream($"JobPortal.Migrations.ConsolidateCv.{phase}.sql")
                ?? throw new InvalidOperationException("Migration SQL resource missing.");
            using var reader = new System.IO.StreamReader(stream);
            return reader.ReadToEnd();
        }
    }
}

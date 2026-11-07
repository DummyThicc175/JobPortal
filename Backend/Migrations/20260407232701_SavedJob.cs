using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobPortal.Migrations
{
    /// <inheritdoc />
    public partial class SavedJob : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SavedJobs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CandidateId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    JobPostId = table.Column<int>(type: "int", nullable: false),
                    SavedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SavedJobs_JobPosts_JobPostId",
                        column: x => x.JobPostId,
                        principalTable: "JobPosts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "b74ddd14-6340-4840-95c2-db12554843e5",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "bb21bf1a-2c95-4cb4-9513-a3bfcb18e1a3", "AQAAAAIAAYagAAAAEP66dZ8fMQrYqMfYUJDPV1ylrVW5wFmU5jVndqOdCrTsbMCxsj3PLVw+W2VY1ip4CQ==", "845c3e75-a25b-4b5c-8479-cf371a12ac39" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "c9284cfc-2dc1-460d-8521-396c0966a4f1",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "3f872066-04dd-4b49-9816-a245a2f6a0c5", "AQAAAAIAAYagAAAAEEcNaiilYJEW30sBeMsmrM2VGBhmhCqdrZiic1xTJn6bUCpr2cTK+wCWht0cnJabqg==", "f35c2057-6781-44fc-a471-a4d4aff3bf93" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "d83b7113-a8f8-4035-99a7-a20dd400f6a4",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "99a20091-339c-438c-85d6-2165a930ba00", "AQAAAAIAAYagAAAAEEFwLOQtvhItPkZJx8V4KDreb2BKMNZUHjAi7kKW6RVm1YWZCIk3tE2sFSz9pT57TA==", "f5ef74b4-ab24-4f00-94b3-4bbd55fed23d" });

            migrationBuilder.CreateIndex(
                name: "IX_SavedJobs_JobPostId",
                table: "SavedJobs",
                column: "JobPostId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SavedJobs");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "b74ddd14-6340-4840-95c2-db12554843e5",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "9caa144b-168c-4f22-a627-2933e475ca2b", "AQAAAAIAAYagAAAAEIgmvWuA6xosVbaJtdYGW90dX4/Zg3dOq2e1S/NKNpvZurpBH7IscY/QpMQhWd6Ccw==", "13c2bda2-1b1e-40d7-b13f-d87b4176d874" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "c9284cfc-2dc1-460d-8521-396c0966a4f1",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "e82d0200-0297-422e-b6a6-ef6cd01c805d", "AQAAAAIAAYagAAAAENL2MZcT69vIGjtSZTp1HJ+AiPP43SGFYhi4qEMoNYFkfHCItsRRZeL44/TImi3+UQ==", "597c1715-8f00-4b07-bb77-d59a18e6e759" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "d83b7113-a8f8-4035-99a7-a20dd400f6a4",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "0db7a09c-bba1-44d1-b896-48fe447e1163", "AQAAAAIAAYagAAAAEJChCvxPy1xmAxsGK+ROk6ZNgkuFLFFzFgwYuoCzkxpIgxrNPVMyZmh1acLl7cz6Sw==", "b7e80cf4-4876-43ae-95f3-5a252a3e309f" });
        }
    }
}

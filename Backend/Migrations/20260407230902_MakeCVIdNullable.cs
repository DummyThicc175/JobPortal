using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobPortal.Migrations
{
    /// <inheritdoc />
    public partial class MakeCVIdNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Applications_CVs_CVId",
                table: "Applications");

            migrationBuilder.AlterColumn<int>(
                name: "CVId",
                table: "Applications",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "b74ddd14-6340-4840-95c2-db12554843e5",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "d4a249e3-e5d9-458b-bd83-53805f451e65", "AQAAAAIAAYagAAAAED1hH6lq62eVZYM+VeA6K3USA3R+8n9+gZuSe6TQdxWshSQO+h550V0VYin/1H0rsA==", "101b83a6-73aa-4210-ac25-13b61ab674f4" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "c9284cfc-2dc1-460d-8521-396c0966a4f1",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "c1811616-28dd-4064-8643-a4035f4865b9", "AQAAAAIAAYagAAAAECqS/YL4MKW0bhzZv1PB2K8gj2peAHuk8u6vaez9dIuEoNk7O/kc4015v/Ed4+g53A==", "23a012e7-f62e-413b-825b-b3f6762114f6" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "d83b7113-a8f8-4035-99a7-a20dd400f6a4",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "7c9577bf-522f-465a-9837-9d492ef53aee", "AQAAAAIAAYagAAAAEKStqK/rQ4VfvJ0GLCH9ACmLBZC7wIWkLKWduWS02fy+pnK1X5yL1KO/I/Okc22rnA==", "78883d7c-7966-4724-88c2-1fc443f24458" });

            migrationBuilder.AddForeignKey(
                name: "FK_Applications_CVs_CVId",
                table: "Applications",
                column: "CVId",
                principalTable: "CVs",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Applications_CVs_CVId",
                table: "Applications");

            migrationBuilder.AlterColumn<int>(
                name: "CVId",
                table: "Applications",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "b74ddd14-6340-4840-95c2-db12554843e5",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "0a55a4bc-a6dd-4cba-9573-1a4b582d0005", "AQAAAAIAAYagAAAAEA325iizvA61b+YzhZmnFUDipZdIhBl21Mxur4ZYt79EWk+IXUC3ljzBm6jxSvTScA==", "cfc997f9-c2b9-4a2e-8498-9ab3cfe010e4" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "c9284cfc-2dc1-460d-8521-396c0966a4f1",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "c643469b-9f7d-4580-a885-c765be7f97a3", "AQAAAAIAAYagAAAAEEFTG78SLv5u1QrqR/Axlac4lh7Izmh/GcQvaeMA8A098SOabXbXGsqYrkORGn9hlw==", "42670bbd-3b24-4bd0-a6c5-9bbf8c880ccb" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "d83b7113-a8f8-4035-99a7-a20dd400f6a4",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "4f3cdc1f-eb0d-49bc-a4a3-a083804f042f", "AQAAAAIAAYagAAAAEIM4X6cmV/czWwax/m3keBj0ozuVNeixbSLBg8NlSOEqln9HuG7Fgh8uu0xLNekNmQ==", "dd293037-743d-4a25-87f6-327a1d0bcda1" });

            migrationBuilder.AddForeignKey(
                name: "FK_Applications_CVs_CVId",
                table: "Applications",
                column: "CVId",
                principalTable: "CVs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

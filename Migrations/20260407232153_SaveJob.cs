using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobPortal.Migrations
{
    /// <inheritdoc />
    public partial class SaveJob : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
        }
    }
}

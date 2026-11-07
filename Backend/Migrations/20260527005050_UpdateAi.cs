using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobPortal.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "b74ddd14-6340-4840-95c2-db12554843e5",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "baa10bc3-20bf-4d89-8907-78dc82d7bbeb", "AQAAAAIAAYagAAAAEJgcDjpbg8ztKCooFYOYfNn0fm4S4rqi8tIe/C1f4KSNlyQtoOV5nEjtAm860P3MUA==", "9ee6378f-ef6d-40b9-8a22-279d7bcec5b5" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "c9284cfc-2dc1-460d-8521-396c0966a4f1",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "8bb6a4c8-22f7-4f22-bb46-f1a225a56b2b", "AQAAAAIAAYagAAAAEMulEQM985vjfBLoozHC3hJX+1IxlyjQ16Kp37GJ+6hE38QtMQuYfzRNGIbn/Tj+FA==", "ec39a2f2-61d1-4655-bbc6-cb9dddda1c15" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "d83b7113-a8f8-4035-99a7-a20dd400f6a4",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "298460db-13f3-427a-8cc2-74e6421fea2c", "AQAAAAIAAYagAAAAEOjhd8FhUHCYScZqp7C3c1XB3GwlFOvT2iMT2i2qqYlpR41/DIGHo7JiE28nPsGJ7g==", "2e75ba8e-6545-4545-8770-ae35abe5ec44" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
    }
}

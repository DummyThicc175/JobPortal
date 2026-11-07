using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobPortal.Migrations
{
    /// <inheritdoc />
    public partial class AddChatTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
        }
    }
}

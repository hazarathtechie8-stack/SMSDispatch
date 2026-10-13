using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SmsDispatch.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Messages",
                columns: table => new
                {
                    Id = table.Column<decimal>(type: "numeric(18,0)", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    To = table.Column<decimal>(type: "numeric(18,0)", nullable: false),
                    From = table.Column<decimal>(type: "numeric(18,0)", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDatetime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RecipientName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ExternalSid = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Messages", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Messages",
                columns: new[] { "Id", "CreatedDateTime", "ErrorMessage", "ExternalSid", "From", "Message", "ModifiedDatetime", "RecipientName", "Status", "To" },
                values: new object[,]
                {
                    { 1m, new DateTime(2025, 1, 15, 10, 0, 0, 0, DateTimeKind.Utc), null, null, 15559876543m, "Hello Alice, your appointment is confirmed.", new DateTime(2025, 1, 15, 10, 0, 4, 0, DateTimeKind.Utc), "Alice", "Sent", 15551234567m },
                    { 2m, new DateTime(2025, 1, 15, 10, 1, 0, 0, DateTimeKind.Utc), null, null, 15559876543m, "Hi Bob, reminder: meeting at 3pm.", new DateTime(2025, 1, 15, 10, 1, 5, 0, DateTimeKind.Utc), "Bob", "Sent", 15552345678m },
                    { 3m, new DateTime(2025, 1, 15, 10, 2, 0, 0, DateTimeKind.Utc), "Not Sent – Not a valid phone", null, 15559876543m, "Carol, your package has been delivered.", new DateTime(2025, 1, 15, 10, 2, 3, 0, DateTimeKind.Utc), "Carol", "Failed", 15553456789m },
                    { 4m, new DateTime(2025, 1, 15, 10, 3, 0, 0, DateTimeKind.Utc), null, null, 15559876543m, "David, your order is ready for pickup.", new DateTime(2025, 1, 15, 10, 3, 0, 0, DateTimeKind.Utc), null, "Pending", 15554567890m },
                    { 5m, new DateTime(2025, 1, 15, 10, 4, 0, 0, DateTimeKind.Utc), null, null, 15559876543m, "Eve, your verification code is 847291.", new DateTime(2025, 1, 15, 10, 4, 0, 0, DateTimeKind.Utc), null, "Queued", 15555678901m }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Messages_CreatedDateTime",
                table: "Messages",
                column: "CreatedDateTime");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_Status",
                table: "Messages",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Messages");
        }
    }
}

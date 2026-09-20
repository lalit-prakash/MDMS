using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MDMS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDownloadRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DownloadRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    RequestPath = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FileName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Content = table.Column<byte[]>(type: "bytea", nullable: true),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: true),
                    RowCount = table.Column<int>(type: "integer", nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DownloadRequests", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DownloadRequests_Status",
                table: "DownloadRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_DownloadRequests_UserId_CreatedAtUtc",
                table: "DownloadRequests",
                columns: new[] { "UserId", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DownloadRequests");
        }
    }
}

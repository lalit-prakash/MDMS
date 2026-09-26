using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MDMS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMeterTestingAndServiceRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MeterTestingRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestNumber = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    MeterId = table.Column<Guid>(type: "uuid", nullable: false),
                    Reason = table.Column<int>(type: "integer", nullable: false),
                    ConsumerRemarks = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ScheduledAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TestResult = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AccuracyResult = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FinalRemarks = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeterTestingRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ServiceRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestNumber = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ResolutionNote = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceRequests", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MeterTestingRequests_CustomerId",
                table: "MeterTestingRequests",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_MeterTestingRequests_RequestNumber",
                table: "MeterTestingRequests",
                column: "RequestNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRequests_CustomerId",
                table: "ServiceRequests",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRequests_RequestNumber",
                table: "ServiceRequests",
                column: "RequestNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MeterTestingRequests");

            migrationBuilder.DropTable(
                name: "ServiceRequests");
        }
    }
}

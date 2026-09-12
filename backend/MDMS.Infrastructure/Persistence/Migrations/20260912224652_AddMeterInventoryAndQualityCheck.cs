using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MDMS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMeterInventoryAndQualityCheck : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InstallationQualityChecks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MeterId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    L1CompletedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    L1CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    L2DecisionByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    L2DecisionAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    L2RejectionNote = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    L3DecisionByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    L3DecisionAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    L3RejectionNote = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InstallationQualityChecks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MeterInventoryRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MeterId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ContractorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    InstallerUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReplacementReason = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeterInventoryRecords", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InstallationQualityChecks_MeterId",
                table: "InstallationQualityChecks",
                column: "MeterId");

            migrationBuilder.CreateIndex(
                name: "IX_MeterInventoryRecords_MeterId",
                table: "MeterInventoryRecords",
                column: "MeterId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InstallationQualityChecks");

            migrationBuilder.DropTable(
                name: "MeterInventoryRecords");
        }
    }
}

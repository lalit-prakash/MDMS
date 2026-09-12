using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MDMS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRevenueProtection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RevenueProtectionLeads",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    MeterId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RiskScore = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    AssignedToUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    FieldFindingNote = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ActionTaken = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    RecoveryAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    ClosureReason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RevenueProtectionLeads", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RiskSignals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LeadId = table.Column<Guid>(type: "uuid", nullable: false),
                    SignalType = table.Column<int>(type: "integer", nullable: false),
                    Weight = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    Note = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RiskSignals", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RevenueProtectionLeads_CustomerId",
                table: "RevenueProtectionLeads",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_RevenueProtectionLeads_Status",
                table: "RevenueProtectionLeads",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_RiskSignals_LeadId",
                table: "RiskSignals",
                column: "LeadId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RevenueProtectionLeads");

            migrationBuilder.DropTable(
                name: "RiskSignals");
        }
    }
}

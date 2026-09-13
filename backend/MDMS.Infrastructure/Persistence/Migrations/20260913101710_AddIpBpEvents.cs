using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MDMS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIpBpEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "KvahExport",
                table: "DailyLoadProfiles",
                type: "numeric(18,4)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "KvahImport",
                table: "DailyLoadProfiles",
                type: "numeric(18,4)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "KwhExport",
                table: "DailyLoadProfiles",
                type: "numeric(18,4)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BillingProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MeterId = table.Column<Guid>(type: "uuid", nullable: false),
                    BillingDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CumulativeKwhImport = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    CumulativeKvahImport = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    CumulativeKwhExport = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    CumulativeKvahExport = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    AveragePowerFactor = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    KwhByTariffZone = table.Column<decimal[]>(type: "numeric(18,4)[]", nullable: false),
                    KvahByTariffZone = table.Column<decimal[]>(type: "numeric(18,4)[]", nullable: false),
                    MaximumDemandKw = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    MaximumDemandKva = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    BillingPowerOnDurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillingProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InstantaneousProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MeterId = table.Column<Guid>(type: "uuid", nullable: false),
                    MeterTimeUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Voltage = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    PhaseCurrent = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    NeutralCurrent = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    PowerFactor = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    Frequency = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    Kw = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    Kva = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    Kwh = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    Kvah = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    KwhExport = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    KvahExport = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    MdKw = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    MdKwAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MdKva = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    MdKvaAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MdKwExport = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    MdKwExportAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MdKvaExport = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    MdKvaExportAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PowerOnDurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    TamperCount = table.Column<int>(type: "integer", nullable: false),
                    BillingCount = table.Column<int>(type: "integer", nullable: false),
                    ProgrammingCount = table.Column<int>(type: "integer", nullable: false),
                    LoadLimitState = table.Column<int>(type: "integer", nullable: false),
                    LoadLimitValue = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InstantaneousProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MeterEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MeterId = table.Column<Guid>(type: "uuid", nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EventType = table.Column<int>(type: "integer", nullable: false),
                    Severity = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsAcknowledged = table.Column<bool>(type: "boolean", nullable: false),
                    AcknowledgedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    AcknowledgedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeterEvents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BillingProfiles_MeterId_BillingDate",
                table: "BillingProfiles",
                columns: new[] { "MeterId", "BillingDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InstantaneousProfiles_MeterId_MeterTimeUtc",
                table: "InstantaneousProfiles",
                columns: new[] { "MeterId", "MeterTimeUtc" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InstantaneousProfiles_MeterTimeUtc",
                table: "InstantaneousProfiles",
                column: "MeterTimeUtc");

            migrationBuilder.CreateIndex(
                name: "IX_MeterEvents_IsAcknowledged",
                table: "MeterEvents",
                column: "IsAcknowledged");

            migrationBuilder.CreateIndex(
                name: "IX_MeterEvents_MeterId",
                table: "MeterEvents",
                column: "MeterId");

            migrationBuilder.CreateIndex(
                name: "IX_MeterEvents_OccurredAtUtc",
                table: "MeterEvents",
                column: "OccurredAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BillingProfiles");

            migrationBuilder.DropTable(
                name: "InstantaneousProfiles");

            migrationBuilder.DropTable(
                name: "MeterEvents");

            migrationBuilder.DropColumn(
                name: "KvahExport",
                table: "DailyLoadProfiles");

            migrationBuilder.DropColumn(
                name: "KvahImport",
                table: "DailyLoadProfiles");

            migrationBuilder.DropColumn(
                name: "KwhExport",
                table: "DailyLoadProfiles");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MDMS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DailyLoadProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServicePointId = table.Column<Guid>(type: "uuid", nullable: false),
                    MeterId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfileDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ConsumptionKwh = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    Source = table.Column<int>(type: "integer", nullable: false),
                    Quality = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyLoadProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DataQualityHolds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MeterId = table.Column<Guid>(type: "uuid", nullable: false),
                    Reason = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    RaisedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    ClearedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResolutionNote = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataQualityHolds", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LoadSurveyIntervals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MeterId = table.Column<Guid>(type: "uuid", nullable: false),
                    IntervalStartUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IntervalEndUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CumulativeReading = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    ConsumptionKwh = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    Quality = table.Column<int>(type: "integer", nullable: false),
                    Source = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoadSurveyIntervals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Meters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SerialNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Phase = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Meters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ServicePoints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Address = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServicePoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServicePoints_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MeterAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServicePointId = table.Column<Guid>(type: "uuid", nullable: false),
                    MeterId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<int>(type: "integer", nullable: false),
                    EffectiveFromUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EffectiveToUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClosingReading = table.Column<decimal>(type: "numeric", nullable: true),
                    OpeningReading = table.Column<decimal>(type: "numeric", nullable: true),
                    Reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeterAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MeterAssignments_Meters_MeterId",
                        column: x => x.MeterId,
                        principalTable: "Meters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Customers_AccountNumber",
                table: "Customers",
                column: "AccountNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DailyLoadProfiles_ServicePointId_MeterId_ProfileDate",
                table: "DailyLoadProfiles",
                columns: new[] { "ServicePointId", "MeterId", "ProfileDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DataQualityHolds_MeterId_IsActive",
                table: "DataQualityHolds",
                columns: new[] { "MeterId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_LoadSurveyIntervals_MeterId_IntervalStartUtc_IntervalEndUtc",
                table: "LoadSurveyIntervals",
                columns: new[] { "MeterId", "IntervalStartUtc", "IntervalEndUtc" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MeterAssignments_MeterId",
                table: "MeterAssignments",
                column: "MeterId");

            migrationBuilder.CreateIndex(
                name: "IX_MeterAssignments_ServicePointId",
                table: "MeterAssignments",
                column: "ServicePointId");

            migrationBuilder.CreateIndex(
                name: "IX_Meters_SerialNumber",
                table: "Meters",
                column: "SerialNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServicePoints_CustomerId",
                table: "ServicePoints",
                column: "CustomerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DailyLoadProfiles");

            migrationBuilder.DropTable(
                name: "DataQualityHolds");

            migrationBuilder.DropTable(
                name: "LoadSurveyIntervals");

            migrationBuilder.DropTable(
                name: "MeterAssignments");

            migrationBuilder.DropTable(
                name: "ServicePoints");

            migrationBuilder.DropTable(
                name: "Meters");

            migrationBuilder.DropTable(
                name: "Customers");
        }
    }
}

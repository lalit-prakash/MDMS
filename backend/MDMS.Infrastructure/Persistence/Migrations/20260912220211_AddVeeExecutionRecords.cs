using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MDMS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVeeExecutionRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VeeExecutionRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RuleName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    MeasurementType = table.Column<int>(type: "integer", nullable: false),
                    MeterId = table.Column<Guid>(type: "uuid", nullable: false),
                    SlotStartUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SlotEndUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ResultQuality = table.Column<int>(type: "integer", nullable: false),
                    NewValue = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    Details = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VeeExecutionRecords", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VeeExecutionRecords_MeterId_SlotStartUtc",
                table: "VeeExecutionRecords",
                columns: new[] { "MeterId", "SlotStartUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VeeExecutionRecords");
        }
    }
}

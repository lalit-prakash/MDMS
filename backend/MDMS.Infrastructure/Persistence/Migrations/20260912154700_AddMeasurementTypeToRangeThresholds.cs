using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MDMS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMeasurementTypeToRangeThresholds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MeasurementRangeThresholds_MeterId",
                table: "MeasurementRangeThresholds");

            migrationBuilder.AddColumn<int>(
                name: "MeasurementType",
                table: "MeasurementRangeThresholds",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_MeasurementRangeThresholds_MeasurementType_MeterId",
                table: "MeasurementRangeThresholds",
                columns: new[] { "MeasurementType", "MeterId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MeasurementRangeThresholds_MeasurementType_MeterId",
                table: "MeasurementRangeThresholds");

            migrationBuilder.DropColumn(
                name: "MeasurementType",
                table: "MeasurementRangeThresholds");

            migrationBuilder.CreateIndex(
                name: "IX_MeasurementRangeThresholds_MeterId",
                table: "MeasurementRangeThresholds",
                column: "MeterId");
        }
    }
}

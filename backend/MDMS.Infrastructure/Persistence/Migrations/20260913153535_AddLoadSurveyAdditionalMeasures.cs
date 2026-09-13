using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MDMS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLoadSurveyAdditionalMeasures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AverageCurrent",
                table: "LoadSurveyIntervals",
                type: "numeric(18,4)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AverageVoltage",
                table: "LoadSurveyIntervals",
                type: "numeric(18,4)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CumulativeKvahExport",
                table: "LoadSurveyIntervals",
                type: "numeric(18,4)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CumulativeKvahImport",
                table: "LoadSurveyIntervals",
                type: "numeric(18,4)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CumulativeKwhExport",
                table: "LoadSurveyIntervals",
                type: "numeric(18,4)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AverageCurrent",
                table: "LoadSurveyIntervals");

            migrationBuilder.DropColumn(
                name: "AverageVoltage",
                table: "LoadSurveyIntervals");

            migrationBuilder.DropColumn(
                name: "CumulativeKvahExport",
                table: "LoadSurveyIntervals");

            migrationBuilder.DropColumn(
                name: "CumulativeKvahImport",
                table: "LoadSurveyIntervals");

            migrationBuilder.DropColumn(
                name: "CumulativeKwhExport",
                table: "LoadSurveyIntervals");
        }
    }
}

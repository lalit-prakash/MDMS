using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MDMS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEventOccurrenceReadingsAndResolution : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "OccCurrent",
                table: "MeterEvents",
                type: "numeric(18,4)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OccKwh",
                table: "MeterEvents",
                type: "numeric(18,4)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OccTemperature",
                table: "MeterEvents",
                type: "numeric(18,4)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OccVoltage",
                table: "MeterEvents",
                type: "numeric(18,4)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ResolvedAtUtc",
                table: "MeterEvents",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OccCurrent",
                table: "MeterEvents");

            migrationBuilder.DropColumn(
                name: "OccKwh",
                table: "MeterEvents");

            migrationBuilder.DropColumn(
                name: "OccTemperature",
                table: "MeterEvents");

            migrationBuilder.DropColumn(
                name: "OccVoltage",
                table: "MeterEvents");

            migrationBuilder.DropColumn(
                name: "ResolvedAtUtc",
                table: "MeterEvents");
        }
    }
}

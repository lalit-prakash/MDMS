using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MDMS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRegionAndAssetMasterData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DtrType",
                table: "HierarchyNodes",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalCtRatio",
                table: "HierarchyNodes",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalPtRatio",
                table: "HierarchyNodes",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FeederMode",
                table: "HierarchyNodes",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InstalledBy",
                table: "HierarchyNodes",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "MdmAssetTimestampUtc",
                table: "HierarchyNodes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Mect",
                table: "HierarchyNodes",
                type: "numeric(18,4)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Mept",
                table: "HierarchyNodes",
                type: "numeric(18,4)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MeterMake",
                table: "HierarchyNodes",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MeterSerial",
                table: "HierarchyNodes",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MultiplyingFactor",
                table: "HierarchyNodes",
                type: "numeric(18,4)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Satno",
                table: "HierarchyNodes",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsMrRequiredDone",
                table: "Customers",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "MdmAssetTimestampUtc",
                table: "Customers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MeterMake",
                table: "Customers",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MeterPhase",
                table: "Customers",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "MeterReplacementDate",
                table: "Customers",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MultiplyingFactor",
                table: "Customers",
                type: "numeric(18,4)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Satno",
                table: "Customers",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DtrType",
                table: "HierarchyNodes");

            migrationBuilder.DropColumn(
                name: "ExternalCtRatio",
                table: "HierarchyNodes");

            migrationBuilder.DropColumn(
                name: "ExternalPtRatio",
                table: "HierarchyNodes");

            migrationBuilder.DropColumn(
                name: "FeederMode",
                table: "HierarchyNodes");

            migrationBuilder.DropColumn(
                name: "InstalledBy",
                table: "HierarchyNodes");

            migrationBuilder.DropColumn(
                name: "MdmAssetTimestampUtc",
                table: "HierarchyNodes");

            migrationBuilder.DropColumn(
                name: "Mect",
                table: "HierarchyNodes");

            migrationBuilder.DropColumn(
                name: "Mept",
                table: "HierarchyNodes");

            migrationBuilder.DropColumn(
                name: "MeterMake",
                table: "HierarchyNodes");

            migrationBuilder.DropColumn(
                name: "MeterSerial",
                table: "HierarchyNodes");

            migrationBuilder.DropColumn(
                name: "MultiplyingFactor",
                table: "HierarchyNodes");

            migrationBuilder.DropColumn(
                name: "Satno",
                table: "HierarchyNodes");

            migrationBuilder.DropColumn(
                name: "IsMrRequiredDone",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "MdmAssetTimestampUtc",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "MeterMake",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "MeterPhase",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "MeterReplacementDate",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "MultiplyingFactor",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "Satno",
                table: "Customers");
        }
    }
}

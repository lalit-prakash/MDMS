using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MDMS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHierarchyAndConsumerMasterData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CapacityKva",
                table: "HierarchyNodes",
                type: "numeric(18,4)",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "CommissionedOn",
                table: "HierarchyNodes",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Latitude",
                table: "HierarchyNodes",
                type: "numeric(9,6)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Longitude",
                table: "HierarchyNodes",
                type: "numeric(9,6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Make",
                table: "HierarchyNodes",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OperationalStatus",
                table: "HierarchyNodes",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OrgUnitId",
                table: "HierarchyNodes",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VoltageLevel",
                table: "HierarchyNodes",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BillCycle",
                table: "Customers",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BillDay",
                table: "Customers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CommunicationType",
                table: "Customers",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ConnectedLoadKw",
                table: "Customers",
                type: "numeric(18,4)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConnectionStatus",
                table: "Customers",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ContractDemandKva",
                table: "Customers",
                type: "numeric(18,4)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsNetMeter",
                table: "Customers",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Latitude",
                table: "Customers",
                type: "numeric(9,6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LoadType",
                table: "Customers",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Longitude",
                table: "Customers",
                type: "numeric(9,6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MobileNumber",
                table: "Customers",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentMode",
                table: "Customers",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RrNumber",
                table: "Customers",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SanctionedLoadKw",
                table: "Customers",
                type: "numeric(18,4)",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "ServiceDate",
                table: "Customers",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TariffCategoryCode",
                table: "Customers",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CapacityKva",
                table: "HierarchyNodes");

            migrationBuilder.DropColumn(
                name: "CommissionedOn",
                table: "HierarchyNodes");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "HierarchyNodes");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "HierarchyNodes");

            migrationBuilder.DropColumn(
                name: "Make",
                table: "HierarchyNodes");

            migrationBuilder.DropColumn(
                name: "OperationalStatus",
                table: "HierarchyNodes");

            migrationBuilder.DropColumn(
                name: "OrgUnitId",
                table: "HierarchyNodes");

            migrationBuilder.DropColumn(
                name: "VoltageLevel",
                table: "HierarchyNodes");

            migrationBuilder.DropColumn(
                name: "BillCycle",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "BillDay",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "CommunicationType",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "ConnectedLoadKw",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "ConnectionStatus",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "ContractDemandKva",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "IsNetMeter",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "LoadType",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "MobileNumber",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "PaymentMode",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "RrNumber",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "SanctionedLoadKw",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "ServiceDate",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "TariffCategoryCode",
                table: "Customers");
        }
    }
}

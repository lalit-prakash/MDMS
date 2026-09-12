using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MDMS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantConfigHierarchyAndRbac : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_MeasurementRangeThresholds",
                table: "MeasurementRangeThresholds");

            migrationBuilder.RenameTable(
                name: "MeasurementRangeThresholds",
                newName: "VeeRuleDefinitions");

            migrationBuilder.RenameIndex(
                name: "IX_MeasurementRangeThresholds_MeasurementType_MeterId",
                table: "VeeRuleDefinitions",
                newName: "IX_VeeRuleDefinitions_MeasurementType_MeterId");

            migrationBuilder.AddColumn<Guid>(
                name: "DistributionTransformerNodeId",
                table: "ServicePoints",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "ServicePoints",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Meters",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "MeterAssignments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "LoadSurveyIntervals",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "DataQualityHolds",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "DailyLoadProfiles",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Customers",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<decimal>(
                name: "MinConsumptionKwh",
                table: "VeeRuleDefinitions",
                type: "numeric(18,4)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)");

            migrationBuilder.AlterColumn<int>(
                name: "MeasurementType",
                table: "VeeRuleDefinitions",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<decimal>(
                name: "MaxConsumptionKwh",
                table: "VeeRuleDefinitions",
                type: "numeric(18,4)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "VeeRuleDefinitions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "RuleType",
                table: "VeeRuleDefinitions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "VeeRuleDefinitions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddPrimaryKey(
                name: "PK_VeeRuleDefinitions",
                table: "VeeRuleDefinitions",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "HierarchyNodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NodeType = table.Column<int>(type: "integer", nullable: false),
                    Code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ParentId = table.Column<Guid>(type: "uuid", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HierarchyNodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HierarchyNodes_HierarchyNodes_ParentId",
                        column: x => x.ParentId,
                        principalTable: "HierarchyNodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrgUnits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitType = table.Column<int>(type: "integer", nullable: false),
                    Code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ParentId = table.Column<Guid>(type: "uuid", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrgUnits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrgUnits_OrgUnits_ParentId",
                        column: x => x.ParentId,
                        principalTable: "OrgUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TariffCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TariffCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tenants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Username = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    OrgUnitId = table.Column<Guid>(type: "uuid", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServicePoints_DistributionTransformerNodeId",
                table: "ServicePoints",
                column: "DistributionTransformerNodeId");

            migrationBuilder.CreateIndex(
                name: "IX_VeeRuleDefinitions_TenantId",
                table: "VeeRuleDefinitions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_HierarchyNodes_ParentId",
                table: "HierarchyNodes",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_HierarchyNodes_TenantId_NodeType_Code",
                table: "HierarchyNodes",
                columns: new[] { "TenantId", "NodeType", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrgUnits_ParentId",
                table: "OrgUnits",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_OrgUnits_TenantId_UnitType_Code",
                table: "OrgUnits",
                columns: new[] { "TenantId", "UnitType", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TariffCategories_TenantId_Code",
                table: "TariffCategories",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_Code",
                table: "Tenants",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_OrgUnitId",
                table: "Users",
                column: "OrgUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_TenantId_Username",
                table: "Users",
                columns: new[] { "TenantId", "Username" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HierarchyNodes");

            migrationBuilder.DropTable(
                name: "OrgUnits");

            migrationBuilder.DropTable(
                name: "TariffCategories");

            migrationBuilder.DropTable(
                name: "Tenants");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropIndex(
                name: "IX_ServicePoints_DistributionTransformerNodeId",
                table: "ServicePoints");

            migrationBuilder.DropPrimaryKey(
                name: "PK_VeeRuleDefinitions",
                table: "VeeRuleDefinitions");

            migrationBuilder.DropIndex(
                name: "IX_VeeRuleDefinitions_TenantId",
                table: "VeeRuleDefinitions");

            migrationBuilder.DropColumn(
                name: "DistributionTransformerNodeId",
                table: "ServicePoints");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "ServicePoints");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Meters");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "MeterAssignments");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "LoadSurveyIntervals");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "DataQualityHolds");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "DailyLoadProfiles");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "VeeRuleDefinitions");

            migrationBuilder.DropColumn(
                name: "RuleType",
                table: "VeeRuleDefinitions");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "VeeRuleDefinitions");

            migrationBuilder.RenameTable(
                name: "VeeRuleDefinitions",
                newName: "MeasurementRangeThresholds");

            migrationBuilder.RenameIndex(
                name: "IX_VeeRuleDefinitions_MeasurementType_MeterId",
                table: "MeasurementRangeThresholds",
                newName: "IX_MeasurementRangeThresholds_MeasurementType_MeterId");

            migrationBuilder.AlterColumn<decimal>(
                name: "MinConsumptionKwh",
                table: "MeasurementRangeThresholds",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "MeasurementType",
                table: "MeasurementRangeThresholds",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "MaxConsumptionKwh",
                table: "MeasurementRangeThresholds",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_MeasurementRangeThresholds",
                table: "MeasurementRangeThresholds",
                column: "Id");
        }
    }
}

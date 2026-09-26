using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MDMS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiTenancy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The built-in default organisation (well-known id every existing row already carries).
            // Idempotent: a row may already exist in databases created before tenancy was enforced.
            migrationBuilder.Sql(@"INSERT INTO ""Tenants"" (""Id"", ""CreatedAtUtc"", ""TenantId"", ""Code"", ""Name"")
SELECT '00000000-0000-0000-0000-000000000001', NOW() AT TIME ZONE 'utc', '00000000-0000-0000-0000-000000000001', 'DEFAULT', 'Default Organisation'
WHERE NOT EXISTS (SELECT 1 FROM ""Tenants"" WHERE ""Id"" = '00000000-0000-0000-0000-000000000001' OR ""Code"" = 'DEFAULT');");

            migrationBuilder.DropIndex(
                name: "IX_Customers_AccountNumber",
                table: "Customers");

            migrationBuilder.CreateTable(
                name: "UserTenantAccesses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    GrantedTenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTenantAccesses", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Customers_TenantId_AccountNumber",
                table: "Customers",
                columns: new[] { "TenantId", "AccountNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserTenantAccesses_UserId_GrantedTenantId",
                table: "UserTenantAccesses",
                columns: new[] { "UserId", "GrantedTenantId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserTenantAccesses");

            migrationBuilder.DropIndex(
                name: "IX_Customers_TenantId_AccountNumber",
                table: "Customers");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_AccountNumber",
                table: "Customers",
                column: "AccountNumber",
                unique: true);
        }
    }
}

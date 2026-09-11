using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bms.Store.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStoreIdToStoreTenantSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_StoreTenantSettings_Tenant",
                schema: "bms_store",
                table: "StoreTenantSettings");

            migrationBuilder.AddColumn<string>(
                name: "StoreCode",
                schema: "bms_store",
                table: "StoreTenantSettings",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "StoreId",
                schema: "bms_store",
                table: "StoreTenantSettings",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "UX_StoreTenantSettings_Tenant_Store",
                schema: "bms_store",
                table: "StoreTenantSettings",
                columns: new[] { "TenantId", "StoreId" },
                unique: true,
                filter: "\"IsDeleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_StoreTenantSettings_Tenant_Store",
                schema: "bms_store",
                table: "StoreTenantSettings");

            migrationBuilder.DropColumn(
                name: "StoreCode",
                schema: "bms_store",
                table: "StoreTenantSettings");

            migrationBuilder.DropColumn(
                name: "StoreId",
                schema: "bms_store",
                table: "StoreTenantSettings");

            migrationBuilder.CreateIndex(
                name: "UX_StoreTenantSettings_Tenant",
                schema: "bms_store",
                table: "StoreTenantSettings",
                column: "TenantId",
                unique: true,
                filter: "\"IsDeleted\" = false");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bms.Store.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCustomerBeautyProfileStoreIsolation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CustomerBeautyProfiles_TenantId_StoreId",
                schema: "bms_store",
                table: "CustomerBeautyProfiles");

            migrationBuilder.CreateIndex(
                name: "UX_CustomerBeautyProfiles_Tenant_Store_Customer",
                schema: "bms_store",
                table: "CustomerBeautyProfiles",
                columns: new[] { "TenantId", "StoreId", "CustomerId" },
                unique: true,
                filter: "\"IsDeleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_CustomerBeautyProfiles_Tenant_Store_Customer",
                schema: "bms_store",
                table: "CustomerBeautyProfiles");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerBeautyProfiles_TenantId_StoreId",
                schema: "bms_store",
                table: "CustomerBeautyProfiles",
                columns: new[] { "TenantId", "StoreId" });
        }
    }
}

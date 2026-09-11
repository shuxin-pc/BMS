using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bms.Store.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProductTypeToProductSalesStatUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProductSalesStats_TenantId_StoreId_StatDate_ProductId",
                schema: "bms_store",
                table: "ProductSalesStats");

            migrationBuilder.CreateIndex(
                name: "IX_ProductSalesStats_TenantId_StoreId_StatDate_ProductId_Produ~",
                schema: "bms_store",
                table: "ProductSalesStats",
                columns: new[] { "TenantId", "StoreId", "StatDate", "ProductId", "ProductType" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProductSalesStats_TenantId_StoreId_StatDate_ProductId_Produ~",
                schema: "bms_store",
                table: "ProductSalesStats");

            migrationBuilder.CreateIndex(
                name: "IX_ProductSalesStats_TenantId_StoreId_StatDate_ProductId",
                schema: "bms_store",
                table: "ProductSalesStats",
                columns: new[] { "TenantId", "StoreId", "StatDate", "ProductId" },
                unique: true);
        }
    }
}

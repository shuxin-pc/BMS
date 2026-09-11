using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bms.Store.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderNoStoreUnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Orders_OrderNo",
                schema: "bms_store",
                table: "Orders");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_OrderNo",
                schema: "bms_store",
                table: "Orders",
                column: "OrderNo");

            migrationBuilder.CreateIndex(
                name: "UX_Orders_Tenant_Store_OrderNo",
                schema: "bms_store",
                table: "Orders",
                columns: new[] { "TenantId", "StoreId", "OrderNo" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Orders_OrderNo",
                schema: "bms_store",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "UX_Orders_Tenant_Store_OrderNo",
                schema: "bms_store",
                table: "Orders");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_OrderNo",
                schema: "bms_store",
                table: "Orders",
                column: "OrderNo",
                unique: true);
        }
    }
}

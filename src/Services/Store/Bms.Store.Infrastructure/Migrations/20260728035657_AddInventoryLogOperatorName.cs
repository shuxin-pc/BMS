using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bms.Store.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryLogOperatorName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OperatorName",
                schema: "bms_store",
                table: "InventoryLogs",
                type: "text",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryLogs_Suppliers_SupplierId",
                schema: "bms_store",
                table: "InventoryLogs",
                column: "SupplierId",
                principalSchema: "bms_store",
                principalTable: "Suppliers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InventoryLogs_Suppliers_SupplierId",
                schema: "bms_store",
                table: "InventoryLogs");

            migrationBuilder.DropColumn(
                name: "OperatorName",
                schema: "bms_store",
                table: "InventoryLogs");
        }
    }
}

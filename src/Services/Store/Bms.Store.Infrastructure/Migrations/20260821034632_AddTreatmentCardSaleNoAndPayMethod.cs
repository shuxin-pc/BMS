using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bms.Store.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTreatmentCardSaleNoAndPayMethod : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CashAmount",
                schema: "bms_store",
                table: "TreatmentCardSales",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CashPayMethod",
                schema: "bms_store",
                table: "TreatmentCardSales",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PayMethod",
                schema: "bms_store",
                table: "TreatmentCardSales",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PointsAmount",
                schema: "bms_store",
                table: "TreatmentCardSales",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SaleNo",
                schema: "bms_store",
                table: "TreatmentCardSales",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "StoredValueAmount",
                schema: "bms_store",
                table: "TreatmentCardSales",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "UX_TreatmentCardSales_Tenant_Store_SaleNo",
                schema: "bms_store",
                table: "TreatmentCardSales",
                columns: new[] { "TenantId", "StoreId", "SaleNo" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_TreatmentCardSales_Tenant_Store_SaleNo",
                schema: "bms_store",
                table: "TreatmentCardSales");

            migrationBuilder.DropColumn(
                name: "CashAmount",
                schema: "bms_store",
                table: "TreatmentCardSales");

            migrationBuilder.DropColumn(
                name: "CashPayMethod",
                schema: "bms_store",
                table: "TreatmentCardSales");

            migrationBuilder.DropColumn(
                name: "PayMethod",
                schema: "bms_store",
                table: "TreatmentCardSales");

            migrationBuilder.DropColumn(
                name: "PointsAmount",
                schema: "bms_store",
                table: "TreatmentCardSales");

            migrationBuilder.DropColumn(
                name: "SaleNo",
                schema: "bms_store",
                table: "TreatmentCardSales");

            migrationBuilder.DropColumn(
                name: "StoredValueAmount",
                schema: "bms_store",
                table: "TreatmentCardSales");
        }
    }
}

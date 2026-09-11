using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bms.Store.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCheckoutSessionNo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CheckoutSessionNo",
                schema: "bms_store",
                table: "TreatmentCardVerifies",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CheckoutSessionNo",
                schema: "bms_store",
                table: "TreatmentCardSales",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CheckoutSessionNo",
                schema: "bms_store",
                table: "Orders",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentCardVerifies_CheckoutSessionNo",
                schema: "bms_store",
                table: "TreatmentCardVerifies",
                column: "CheckoutSessionNo");

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentCardSales_CheckoutSessionNo",
                schema: "bms_store",
                table: "TreatmentCardSales",
                column: "CheckoutSessionNo");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CheckoutSessionNo",
                schema: "bms_store",
                table: "Orders",
                column: "CheckoutSessionNo");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TreatmentCardVerifies_CheckoutSessionNo",
                schema: "bms_store",
                table: "TreatmentCardVerifies");

            migrationBuilder.DropIndex(
                name: "IX_TreatmentCardSales_CheckoutSessionNo",
                schema: "bms_store",
                table: "TreatmentCardSales");

            migrationBuilder.DropIndex(
                name: "IX_Orders_CheckoutSessionNo",
                schema: "bms_store",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CheckoutSessionNo",
                schema: "bms_store",
                table: "TreatmentCardVerifies");

            migrationBuilder.DropColumn(
                name: "CheckoutSessionNo",
                schema: "bms_store",
                table: "TreatmentCardSales");

            migrationBuilder.DropColumn(
                name: "CheckoutSessionNo",
                schema: "bms_store",
                table: "Orders");
        }
    }
}

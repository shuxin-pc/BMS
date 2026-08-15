using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bms.Store.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProductIdToServiceReactionAndPhoto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "ProductId",
                schema: "bms_store",
                table: "ServiceReactions",
                type: "bigint",
                nullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "OrderId",
                schema: "bms_store",
                table: "ServiceComparisonPhotos",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddColumn<long>(
                name: "ProductId",
                schema: "bms_store",
                table: "ServiceComparisonPhotos",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceReactions_ProductId",
                schema: "bms_store",
                table: "ServiceReactions",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceComparisonPhotos_ProductId",
                schema: "bms_store",
                table: "ServiceComparisonPhotos",
                column: "ProductId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ServiceReactions_ProductId",
                schema: "bms_store",
                table: "ServiceReactions");

            migrationBuilder.DropIndex(
                name: "IX_ServiceComparisonPhotos_ProductId",
                schema: "bms_store",
                table: "ServiceComparisonPhotos");

            migrationBuilder.DropColumn(
                name: "ProductId",
                schema: "bms_store",
                table: "ServiceReactions");

            migrationBuilder.DropColumn(
                name: "ProductId",
                schema: "bms_store",
                table: "ServiceComparisonPhotos");

            migrationBuilder.AlterColumn<long>(
                name: "OrderId",
                schema: "bms_store",
                table: "ServiceComparisonPhotos",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);
        }
    }
}

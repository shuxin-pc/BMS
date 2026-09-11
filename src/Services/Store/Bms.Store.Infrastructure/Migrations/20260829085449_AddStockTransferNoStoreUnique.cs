using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Bms.Store.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStockTransferNoStoreUnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StockTransfers_TransferNo",
                schema: "bms_store",
                table: "StockTransfers");

            migrationBuilder.CreateTable(
                name: "InventoryCheckBatches",
                schema: "bms_store",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CheckId = table.Column<long>(type: "bigint", nullable: false),
                    BatchId = table.Column<long>(type: "bigint", nullable: false),
                    BatchNo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    ExpirationDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    TenantCode = table.Column<string>(type: "text", nullable: false),
                    StoreId = table.Column<long>(type: "bigint", nullable: false),
                    StoreCode = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryCheckBatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryCheckBatches_InventoryBatches_BatchId",
                        column: x => x.BatchId,
                        principalSchema: "bms_store",
                        principalTable: "InventoryBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryCheckBatches_InventoryChecks_CheckId",
                        column: x => x.CheckId,
                        principalSchema: "bms_store",
                        principalTable: "InventoryChecks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StockTransfers_TransferNo",
                schema: "bms_store",
                table: "StockTransfers",
                column: "TransferNo");

            migrationBuilder.CreateIndex(
                name: "UX_StockTransfers_Tenant_Store_TransferNo",
                schema: "bms_store",
                table: "StockTransfers",
                columns: new[] { "TenantId", "StoreId", "TransferNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCheckBatches_BatchId",
                schema: "bms_store",
                table: "InventoryCheckBatches",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCheckBatches_CheckId",
                schema: "bms_store",
                table: "InventoryCheckBatches",
                column: "CheckId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCheckBatches_TenantId_StoreId",
                schema: "bms_store",
                table: "InventoryCheckBatches",
                columns: new[] { "TenantId", "StoreId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InventoryCheckBatches",
                schema: "bms_store");

            migrationBuilder.DropIndex(
                name: "IX_StockTransfers_TransferNo",
                schema: "bms_store",
                table: "StockTransfers");

            migrationBuilder.DropIndex(
                name: "UX_StockTransfers_Tenant_Store_TransferNo",
                schema: "bms_store",
                table: "StockTransfers");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransfers_TransferNo",
                schema: "bms_store",
                table: "StockTransfers",
                column: "TransferNo",
                unique: true);
        }
    }
}

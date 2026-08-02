using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Bms.Store.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSampleGiftTransfer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SampleGiftTransfers",
                schema: "bms_store",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TransferNo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FromStoreId = table.Column<long>(type: "bigint", nullable: false),
                    FromStoreCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    FromStoreName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ToStoreId = table.Column<long>(type: "bigint", nullable: false),
                    ToStoreCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ToStoreName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TransferDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    OperatorId = table.Column<long>(type: "bigint", nullable: true),
                    OperatorName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_SampleGiftTransfers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SampleGiftTransferItems",
                schema: "bms_store",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SampleGiftTransferId = table.Column<long>(type: "bigint", nullable: false),
                    ProductId = table.Column<long>(type: "bigint", nullable: false),
                    ProductName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ProductCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Unit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    BatchNo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_SampleGiftTransferItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SampleGiftTransferItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "bms_store",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SampleGiftTransferItems_SampleGiftTransfers_SampleGiftTrans~",
                        column: x => x.SampleGiftTransferId,
                        principalSchema: "bms_store",
                        principalTable: "SampleGiftTransfers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SampleGiftTransferItems_ProductId",
                schema: "bms_store",
                table: "SampleGiftTransferItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_SampleGiftTransferItems_SampleGiftTransferId",
                schema: "bms_store",
                table: "SampleGiftTransferItems",
                column: "SampleGiftTransferId");

            migrationBuilder.CreateIndex(
                name: "IX_SampleGiftTransferItems_TenantId_StoreId",
                schema: "bms_store",
                table: "SampleGiftTransferItems",
                columns: new[] { "TenantId", "StoreId" });

            migrationBuilder.CreateIndex(
                name: "IX_SampleGiftTransfers_FromStoreId",
                schema: "bms_store",
                table: "SampleGiftTransfers",
                column: "FromStoreId");

            migrationBuilder.CreateIndex(
                name: "IX_SampleGiftTransfers_Status",
                schema: "bms_store",
                table: "SampleGiftTransfers",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SampleGiftTransfers_TenantId_StoreId_TransferNo",
                schema: "bms_store",
                table: "SampleGiftTransfers",
                columns: new[] { "TenantId", "StoreId", "TransferNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SampleGiftTransfers_ToStoreId",
                schema: "bms_store",
                table: "SampleGiftTransfers",
                column: "ToStoreId");

            migrationBuilder.CreateIndex(
                name: "IX_SampleGiftTransfers_TransferDate",
                schema: "bms_store",
                table: "SampleGiftTransfers",
                column: "TransferDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SampleGiftTransferItems",
                schema: "bms_store");

            migrationBuilder.DropTable(
                name: "SampleGiftTransfers",
                schema: "bms_store");
        }
    }
}

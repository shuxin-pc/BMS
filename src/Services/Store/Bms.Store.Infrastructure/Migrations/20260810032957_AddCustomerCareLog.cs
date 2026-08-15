using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Bms.Store.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerCareLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CustomerCareLogs",
                schema: "bms_store",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustomerId = table.Column<long>(type: "bigint", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    RefOrderId = table.Column<long>(type: "bigint", nullable: true),
                    CareYear = table.Column<int>(type: "integer", nullable: true),
                    Method = table.Column<int>(type: "integer", nullable: true),
                    CareTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
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
                    table.PrimaryKey("PK_CustomerCareLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerCareLogs_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "bms_store",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerCareLogs_CustomerId_Type_CareYear",
                schema: "bms_store",
                table: "CustomerCareLogs",
                columns: new[] { "CustomerId", "Type", "CareYear" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerCareLogs_RefOrderId_Type",
                schema: "bms_store",
                table: "CustomerCareLogs",
                columns: new[] { "RefOrderId", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerCareLogs_TenantId_StoreId",
                schema: "bms_store",
                table: "CustomerCareLogs",
                columns: new[] { "TenantId", "StoreId" });

            migrationBuilder.CreateIndex(
                name: "UX_CustomerCareLogs_BirthdayCare",
                schema: "bms_store",
                table: "CustomerCareLogs",
                columns: new[] { "TenantId", "StoreId", "CustomerId", "CareYear" },
                unique: true,
                filter: "\"Type\" = 1");

            migrationBuilder.CreateIndex(
                name: "UX_CustomerCareLogs_ConsumeThank",
                schema: "bms_store",
                table: "CustomerCareLogs",
                columns: new[] { "TenantId", "StoreId", "RefOrderId" },
                unique: true,
                filter: "\"Type\" = 2");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerCareLogs",
                schema: "bms_store");
        }
    }
}

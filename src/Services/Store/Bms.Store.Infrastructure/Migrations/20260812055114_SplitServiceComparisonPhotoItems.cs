using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Bms.Store.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SplitServiceComparisonPhotoItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PhotoUrl",
                schema: "bms_store",
                table: "ServiceComparisonPhotos");

            migrationBuilder.CreateTable(
                name: "ServiceComparisonPhotoItems",
                schema: "bms_store",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ServiceComparisonPhotoId = table.Column<long>(type: "bigint", nullable: false),
                    PhotoUrl = table.Column<string>(type: "text", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_ServiceComparisonPhotoItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceComparisonPhotoItems_ServiceComparisonPhotos_Service~",
                        column: x => x.ServiceComparisonPhotoId,
                        principalSchema: "bms_store",
                        principalTable: "ServiceComparisonPhotos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceComparisonPhotoItems_ServiceComparisonPhotoId",
                schema: "bms_store",
                table: "ServiceComparisonPhotoItems",
                column: "ServiceComparisonPhotoId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceComparisonPhotoItems_TenantId_StoreId",
                schema: "bms_store",
                table: "ServiceComparisonPhotoItems",
                columns: new[] { "TenantId", "StoreId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ServiceComparisonPhotoItems",
                schema: "bms_store");

            migrationBuilder.AddColumn<string>(
                name: "PhotoUrl",
                schema: "bms_store",
                table: "ServiceComparisonPhotos",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}

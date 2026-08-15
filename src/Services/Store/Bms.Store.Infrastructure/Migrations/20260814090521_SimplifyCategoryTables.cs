using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Bms.Store.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyCategoryTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SkillCategories_Code",
                schema: "bms_store",
                table: "SkillCategories");

            migrationBuilder.DropIndex(
                name: "IX_SkillCategories_Status",
                schema: "bms_store",
                table: "SkillCategories");

            migrationBuilder.DropIndex(
                name: "IX_SkillCategories_TenantId_StoreId",
                schema: "bms_store",
                table: "SkillCategories");

            migrationBuilder.DropColumn(
                name: "Remark",
                schema: "bms_store",
                table: "SkillCategories");

            migrationBuilder.DropColumn(
                name: "Status",
                schema: "bms_store",
                table: "SkillCategories");

            migrationBuilder.DropColumn(
                name: "StoreCode",
                schema: "bms_store",
                table: "SkillCategories");

            migrationBuilder.DropColumn(
                name: "StoreId",
                schema: "bms_store",
                table: "SkillCategories");

            migrationBuilder.DropColumn(
                name: "Remark",
                schema: "bms_store",
                table: "ProductCategories");

            migrationBuilder.DropColumn(
                name: "Sort",
                schema: "bms_store",
                table: "ProductCategories");

            migrationBuilder.CreateTable(
                name: "ServiceProductSkills",
                schema: "bms_store",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ServiceProductId = table.Column<long>(type: "bigint", nullable: false),
                    SkillCategoryId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    TenantCode = table.Column<string>(type: "text", nullable: false),
                    StoreId = table.Column<long>(type: "bigint", nullable: false),
                    StoreCode = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceProductSkills", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceProductSkills_ServiceProducts_ServiceProductId",
                        column: x => x.ServiceProductId,
                        principalSchema: "bms_store",
                        principalTable: "ServiceProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ServiceProductSkills_SkillCategories_SkillCategoryId",
                        column: x => x.SkillCategoryId,
                        principalSchema: "bms_store",
                        principalTable: "SkillCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SkillCategories_Tenant",
                schema: "bms_store",
                table: "SkillCategories",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "UX_SkillCategories_Tenant_Code_Deleted",
                schema: "bms_store",
                table: "SkillCategories",
                columns: new[] { "TenantId", "Code", "IsDeleted" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceProductSkills_ServiceProductId",
                schema: "bms_store",
                table: "ServiceProductSkills",
                column: "ServiceProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceProductSkills_ServiceProductId_StoreId_SkillCategory~",
                schema: "bms_store",
                table: "ServiceProductSkills",
                columns: new[] { "ServiceProductId", "StoreId", "SkillCategoryId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceProductSkills_SkillCategoryId",
                schema: "bms_store",
                table: "ServiceProductSkills",
                column: "SkillCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceProductSkills_TenantId_StoreId",
                schema: "bms_store",
                table: "ServiceProductSkills",
                columns: new[] { "TenantId", "StoreId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ServiceProductSkills",
                schema: "bms_store");

            migrationBuilder.DropIndex(
                name: "IX_SkillCategories_Tenant",
                schema: "bms_store",
                table: "SkillCategories");

            migrationBuilder.DropIndex(
                name: "UX_SkillCategories_Tenant_Code_Deleted",
                schema: "bms_store",
                table: "SkillCategories");

            migrationBuilder.AddColumn<string>(
                name: "Remark",
                schema: "bms_store",
                table: "SkillCategories",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                schema: "bms_store",
                table: "SkillCategories",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "StoreCode",
                schema: "bms_store",
                table: "SkillCategories",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "StoreId",
                schema: "bms_store",
                table: "SkillCategories",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "Remark",
                schema: "bms_store",
                table: "ProductCategories",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Sort",
                schema: "bms_store",
                table: "ProductCategories",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_SkillCategories_Code",
                schema: "bms_store",
                table: "SkillCategories",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_SkillCategories_Status",
                schema: "bms_store",
                table: "SkillCategories",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SkillCategories_TenantId_StoreId",
                schema: "bms_store",
                table: "SkillCategories",
                columns: new[] { "TenantId", "StoreId" });
        }
    }
}

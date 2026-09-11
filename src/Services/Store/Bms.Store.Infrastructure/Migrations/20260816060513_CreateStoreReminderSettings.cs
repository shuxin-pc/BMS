using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Bms.Store.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CreateStoreReminderSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BirthdayReminderRoleIds",
                schema: "bms_store",
                table: "StoreTenantSettings");

            migrationBuilder.CreateTable(
                name: "StoreReminderSettings",
                schema: "bms_store",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ReminderType = table.Column<string>(type: "text", nullable: false),
                    RoleIds = table.Column<List<long>>(type: "jsonb", nullable: true),
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
                    table.PrimaryKey("PK_StoreReminderSettings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "UX_StoreReminderSettings_Tenant_Store_Type",
                schema: "bms_store",
                table: "StoreReminderSettings",
                columns: new[] { "TenantId", "StoreId", "ReminderType" },
                unique: true,
                filter: "\"IsDeleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StoreReminderSettings",
                schema: "bms_store");

            migrationBuilder.AddColumn<List<long>>(
                name: "BirthdayReminderRoleIds",
                schema: "bms_store",
                table: "StoreTenantSettings",
                type: "jsonb",
                nullable: true);
        }
    }
}

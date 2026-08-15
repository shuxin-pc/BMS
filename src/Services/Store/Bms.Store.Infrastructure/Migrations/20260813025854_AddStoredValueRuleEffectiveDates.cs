using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bms.Store.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStoredValueRuleEffectiveDates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GiftRate",
                schema: "bms_store",
                table: "StoredValueRules");

            migrationBuilder.AddColumn<DateTime>(
                name: "EndDate",
                schema: "bms_store",
                table: "StoredValueRules",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartDate",
                schema: "bms_store",
                table: "StoredValueRules",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            // 历史规则原本没有有效期概念（等同于一直生效），用创建时间回填生效日期，
            // 既保持"立即生效"的既有行为，也避免列表展示 0001-01-01
            migrationBuilder.Sql(
                """
                UPDATE bms_store."StoredValueRules"
                SET "StartDate" = "CreatedTime"
                WHERE "StartDate" = '0001-01-01 00:00:00';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EndDate",
                schema: "bms_store",
                table: "StoredValueRules");

            migrationBuilder.DropColumn(
                name: "StartDate",
                schema: "bms_store",
                table: "StoredValueRules");

            migrationBuilder.AddColumn<decimal>(
                name: "GiftRate",
                schema: "bms_store",
                table: "StoredValueRules",
                type: "numeric(5,4)",
                precision: 5,
                scale: 4,
                nullable: true);
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bms.Store.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveStoredValueRuleCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StoredValueRules_Code",
                schema: "bms_store",
                table: "StoredValueRules");

            migrationBuilder.DropColumn(
                name: "Code",
                schema: "bms_store",
                table: "StoredValueRules");

            migrationBuilder.CreateIndex(
                name: "IX_StoredValueRules_TenantId_Amount",
                schema: "bms_store",
                table: "StoredValueRules",
                columns: new[] { "TenantId", "Amount" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StoredValueRules_TenantId_Amount",
                schema: "bms_store",
                table: "StoredValueRules");

            migrationBuilder.AddColumn<string>(
                name: "Code",
                schema: "bms_store",
                table: "StoredValueRules",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_StoredValueRules_Code",
                schema: "bms_store",
                table: "StoredValueRules",
                column: "Code");
        }
    }
}

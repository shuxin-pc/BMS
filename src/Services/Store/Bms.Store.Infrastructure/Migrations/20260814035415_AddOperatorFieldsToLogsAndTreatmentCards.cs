using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bms.Store.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOperatorFieldsToLogsAndTreatmentCards : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OperatorName",
                schema: "bms_store",
                table: "TreatmentCardVerifies",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OperatorName",
                schema: "bms_store",
                table: "TreatmentCardTransfers",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OperatorName",
                schema: "bms_store",
                table: "StoredValueLogs",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "OperatorId",
                schema: "bms_store",
                table: "InventoryLogs",
                type: "bigint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OperatorName",
                schema: "bms_store",
                table: "TreatmentCardVerifies");

            migrationBuilder.DropColumn(
                name: "OperatorName",
                schema: "bms_store",
                table: "TreatmentCardTransfers");

            migrationBuilder.DropColumn(
                name: "OperatorName",
                schema: "bms_store",
                table: "StoredValueLogs");

            migrationBuilder.DropColumn(
                name: "OperatorId",
                schema: "bms_store",
                table: "InventoryLogs");
        }
    }
}

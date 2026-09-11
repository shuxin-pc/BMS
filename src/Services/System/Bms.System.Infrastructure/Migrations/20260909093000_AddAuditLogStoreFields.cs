using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bms.System.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditLogStoreFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "StoreId",
                schema: "bms_system",
                table: "AuditLogs",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StoreName",
                schema: "bms_system",
                table: "AuditLogs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StoreName",
                schema: "bms_system",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "StoreId",
                schema: "bms_system",
                table: "AuditLogs");
        }
    }
}

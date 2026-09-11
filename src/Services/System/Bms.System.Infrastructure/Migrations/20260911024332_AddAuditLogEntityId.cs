using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bms.System.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditLogEntityId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "EntityId",
                schema: "bms_system",
                table: "AuditLogs",
                type: "bigint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EntityId",
                schema: "bms_system",
                table: "AuditLogs");
        }
    }
}

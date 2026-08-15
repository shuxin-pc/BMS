using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bms.Store.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBirthdayReminderRoleIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<List<long>>(
                name: "BirthdayReminderRoleIds",
                schema: "bms_store",
                table: "StoreTenantSettings",
                type: "jsonb",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BirthdayReminderRoleIds",
                schema: "bms_store",
                table: "StoreTenantSettings");
        }
    }
}

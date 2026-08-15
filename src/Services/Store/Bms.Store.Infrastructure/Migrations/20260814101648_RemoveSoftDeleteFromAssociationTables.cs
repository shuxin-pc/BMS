using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bms.Store.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSoftDeleteFromAssociationTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "bms_store",
                table: "TechnicianSkills");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "bms_store",
                table: "ServiceProductSkills");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "bms_store",
                table: "ServiceProductEquipments");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "bms_store",
                table: "TechnicianSkills",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "bms_store",
                table: "ServiceProductSkills",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "bms_store",
                table: "ServiceProductEquipments",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}

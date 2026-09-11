using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bms.Store.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEquipmentTypeParentId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                schema: "bms_store",
                table: "EquipmentTypes");

            migrationBuilder.AddColumn<long>(
                name: "ParentId",
                schema: "bms_store",
                table: "EquipmentTypes",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentTypes_ParentId",
                schema: "bms_store",
                table: "EquipmentTypes",
                column: "ParentId");

            migrationBuilder.AddForeignKey(
                name: "FK_EquipmentTypes_EquipmentTypes_ParentId",
                schema: "bms_store",
                table: "EquipmentTypes",
                column: "ParentId",
                principalSchema: "bms_store",
                principalTable: "EquipmentTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EquipmentTypes_EquipmentTypes_ParentId",
                schema: "bms_store",
                table: "EquipmentTypes");

            migrationBuilder.DropIndex(
                name: "IX_EquipmentTypes_ParentId",
                schema: "bms_store",
                table: "EquipmentTypes");

            migrationBuilder.DropColumn(
                name: "ParentId",
                schema: "bms_store",
                table: "EquipmentTypes");

            migrationBuilder.AddColumn<string>(
                name: "Category",
                schema: "bms_store",
                table: "EquipmentTypes",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);
        }
    }
}

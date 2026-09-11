using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bms.Store.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceTimeAndVerifyResources : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "EquipmentId",
                schema: "bms_store",
                table: "TreatmentCardVerifyItems",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "RoomId",
                schema: "bms_store",
                table: "TreatmentCardVerifyItems",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ServiceEndTime",
                schema: "bms_store",
                table: "TreatmentCardVerifyItems",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ServiceStartTime",
                schema: "bms_store",
                table: "TreatmentCardVerifyItems",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "TechnicianId",
                schema: "bms_store",
                table: "TreatmentCardVerifyItems",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TechnicianSource",
                schema: "bms_store",
                table: "TreatmentCardVerifyItems",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ServiceEndTime",
                schema: "bms_store",
                table: "OrderItems",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ServiceStartTime",
                schema: "bms_store",
                table: "OrderItems",
                type: "timestamp without time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EquipmentId",
                schema: "bms_store",
                table: "TreatmentCardVerifyItems");

            migrationBuilder.DropColumn(
                name: "RoomId",
                schema: "bms_store",
                table: "TreatmentCardVerifyItems");

            migrationBuilder.DropColumn(
                name: "ServiceEndTime",
                schema: "bms_store",
                table: "TreatmentCardVerifyItems");

            migrationBuilder.DropColumn(
                name: "ServiceStartTime",
                schema: "bms_store",
                table: "TreatmentCardVerifyItems");

            migrationBuilder.DropColumn(
                name: "TechnicianId",
                schema: "bms_store",
                table: "TreatmentCardVerifyItems");

            migrationBuilder.DropColumn(
                name: "TechnicianSource",
                schema: "bms_store",
                table: "TreatmentCardVerifyItems");

            migrationBuilder.DropColumn(
                name: "ServiceEndTime",
                schema: "bms_store",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "ServiceStartTime",
                schema: "bms_store",
                table: "OrderItems");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bms.Store.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAppointmentNoStoreUnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Appointments_AppointmentNo",
                schema: "bms_store",
                table: "Appointments");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_AppointmentNo",
                schema: "bms_store",
                table: "Appointments",
                column: "AppointmentNo");

            migrationBuilder.CreateIndex(
                name: "UX_Appointments_Tenant_Store_AppointmentNo",
                schema: "bms_store",
                table: "Appointments",
                columns: new[] { "TenantId", "StoreId", "AppointmentNo" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Appointments_AppointmentNo",
                schema: "bms_store",
                table: "Appointments");

            migrationBuilder.DropIndex(
                name: "UX_Appointments_Tenant_Store_AppointmentNo",
                schema: "bms_store",
                table: "Appointments");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_AppointmentNo",
                schema: "bms_store",
                table: "Appointments",
                column: "AppointmentNo",
                unique: true);
        }
    }
}

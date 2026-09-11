using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bms.Store.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTreatmentCardCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TreatmentCards_Code",
                schema: "bms_store",
                table: "TreatmentCards");

            migrationBuilder.DropColumn(
                name: "Code",
                schema: "bms_store",
                table: "TreatmentCards");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Code",
                schema: "bms_store",
                table: "TreatmentCards",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentCards_Code",
                schema: "bms_store",
                table: "TreatmentCards",
                column: "Code");
        }
    }
}

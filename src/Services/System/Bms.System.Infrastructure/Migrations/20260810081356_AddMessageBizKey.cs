using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bms.System.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMessageBizKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BizKey",
                schema: "bms_system",
                table: "Messages",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BizType",
                schema: "bms_system",
                table: "Messages",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Messages_BizType_BizKey",
                schema: "bms_system",
                table: "Messages",
                columns: new[] { "BizType", "BizKey" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Messages_BizType_BizKey",
                schema: "bms_system",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "BizKey",
                schema: "bms_system",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "BizType",
                schema: "bms_system",
                table: "Messages");
        }
    }
}

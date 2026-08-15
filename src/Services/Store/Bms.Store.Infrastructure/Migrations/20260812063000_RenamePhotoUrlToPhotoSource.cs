using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bms.Store.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenamePhotoUrlToPhotoSource : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 历史明细存的是 base64 DataURL，长度远超 500，不清空则 AlterColumn 直接失败；
            // 且 base64 无法还原成对象存储的 objectKey，保留也读不出图。清空主记录，明细随外键级联删除
            migrationBuilder.Sql(@"DELETE FROM bms_store.""ServiceComparisonPhotos"";");

            migrationBuilder.RenameColumn(
                name: "PhotoUrl",
                schema: "bms_store",
                table: "ServiceComparisonPhotoItems",
                newName: "PhotoSource");

            migrationBuilder.AlterColumn<string>(
                name: "PhotoSource",
                schema: "bms_store",
                table: "ServiceComparisonPhotoItems",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "PhotoSource",
                schema: "bms_store",
                table: "ServiceComparisonPhotoItems",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.RenameColumn(
                name: "PhotoSource",
                schema: "bms_store",
                table: "ServiceComparisonPhotoItems",
                newName: "PhotoUrl");
        }
    }
}

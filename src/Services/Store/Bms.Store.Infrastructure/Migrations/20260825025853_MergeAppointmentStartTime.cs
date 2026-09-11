using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bms.Store.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MergeAppointmentStartTime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 新增一体格式开始时间 StartTime（先 nullable 以便回填旧数据，避免 NOT NULL 约束导致失败）
            migrationBuilder.AddColumn<DateTime>(
                name: "StartTime",
                schema: "bms_store",
                table: "Appointments",
                type: "timestamp without time zone",
                nullable: true);

            // 数据迁移：StartTime = 原 AppointmentDate(日期) + AppointmentTime(时长)
            // PostgreSQL 中 timestamp + interval = timestamp，结果即完整预约开始时刻（跨日时自然进位）
            migrationBuilder.Sql(
                "UPDATE \"bms_store\".\"Appointments\" SET \"StartTime\" = \"AppointmentDate\" + \"AppointmentTime\";");

            // 回填完成后改为非空
            migrationBuilder.AlterColumn<DateTime>(
                name: "StartTime",
                schema: "bms_store",
                table: "Appointments",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);

            // 移除已合并的旧列（DropColumn 同时清理 AppointmentDate 上的 IX_Appointments_AppointmentDate 索引）
            migrationBuilder.DropColumn(
                name: "AppointmentTime",
                schema: "bms_store",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "AppointmentDate",
                schema: "bms_store",
                table: "Appointments");

            // 为新的开始时间列重建索引
            migrationBuilder.CreateIndex(
                name: "IX_Appointments_StartTime",
                schema: "bms_store",
                table: "Appointments",
                column: "StartTime");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 反向：拆回 日期 + 时长 两个旧列（先 nullable 回填，再收紧约束）
            migrationBuilder.AddColumn<DateTime>(
                name: "AppointmentDate",
                schema: "bms_store",
                table: "Appointments",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "AppointmentTime",
                schema: "bms_store",
                table: "Appointments",
                type: "interval",
                nullable: true);

            // 数据迁移：AppointmentDate = 开始日期，AppointmentTime = 开始时刻的时长（当日 0 点起算）
            migrationBuilder.Sql(
                "UPDATE \"bms_store\".\"Appointments\" SET \"AppointmentDate\" = \"StartTime\"::date, \"AppointmentTime\" = \"StartTime\"::time;");

            migrationBuilder.AlterColumn<DateTime>(
                name: "AppointmentDate",
                schema: "bms_store",
                table: "Appointments",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<TimeSpan>(
                name: "AppointmentTime",
                schema: "bms_store",
                table: "Appointments",
                type: "interval",
                nullable: false,
                oldClrType: typeof(TimeSpan),
                oldType: "interval",
                oldNullable: true);

            migrationBuilder.DropIndex(
                name: "IX_Appointments_StartTime",
                schema: "bms_store",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "StartTime",
                schema: "bms_store",
                table: "Appointments");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_AppointmentDate",
                schema: "bms_store",
                table: "Appointments",
                column: "AppointmentDate");
        }
    }
}

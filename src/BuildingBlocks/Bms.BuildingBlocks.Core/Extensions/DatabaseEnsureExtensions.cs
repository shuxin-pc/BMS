using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Bms.BuildingBlocks.Core.Extensions;

/// <summary>
/// 数据库启动初始化扩展：在 Migrate() 之前确保目标数据库存在。
/// 数据库不存在时，EF 的 Migrate() 会先做存在性探测，探测连接失败与空库查询迁移历史表失败
/// 都会以 fail 级日志输出，易被误读为故障。此处将预期路径转为 info 提示并提前建库，
/// 真正的连接故障仍由调用方的 catch 记录完整异常，诊断能力不受影响。
/// </summary>
public static class DatabaseEnsureExtensions
{
    public static void EnsureDatabaseExists(this DatabaseFacade database, ILogger logger)
    {
        var connectionString = database.GetConnectionString();
        var dbName = new NpgsqlConnectionStringBuilder(connectionString).Database;
        if (string.IsNullOrEmpty(dbName))
        {
            return;
        }

        // 连接 postgres 系统库查询 pg_database，探测目标库是否存在
        var postgresConnString = new NpgsqlConnectionStringBuilder(connectionString)
        {
            Database = "postgres"
        }.ConnectionString;
        using var conn = new NpgsqlConnection(postgresConnString);
        conn.Open();
        using var cmd = new NpgsqlCommand(
            "SELECT 1 FROM pg_database WHERE datname = @dbname", conn);
        cmd.Parameters.AddWithValue("dbname", dbName);
        if (cmd.ExecuteScalar() is not null)
        {
            return;
        }

        logger.LogInformation("数据库 {DbName} 不存在，将自动创建", dbName);
        using var createCmd = new NpgsqlCommand(
            $"CREATE DATABASE {new NpgsqlCommandBuilder().QuoteIdentifier(dbName)}", conn);
        createCmd.ExecuteNonQuery();

        // 空库上 Migrate() 查询迁移历史表时会因表不存在再产生一条 fail 日志，
        // 此处用 EF 自身逻辑先建好 __EFMigrationsHistory，使后续迁移全程无错误级噪音
        database.GetService<IHistoryRepository>().Create();
    }
}

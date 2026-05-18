namespace Bms.System.Application.Services;

/// <summary>
/// 子系统数据迁移服务接口
/// </summary>
public interface ISubsystemMigrationService
{
    /// <summary>
    /// 执行数据迁移
    /// </summary>
    Task MigrateAsync();
}

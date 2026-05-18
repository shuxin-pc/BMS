namespace Bms.BuildingBlocks.MultiTenant.Models;

/// <summary>
/// 租户隔离级别
/// </summary>
public enum TenantIsolationLevel
{
    /// <summary>
    /// 行级隔离：共享数据库、共享Schema，通过租户ID区分
    /// </summary>
    Row = 1,

    /// <summary>
    /// Schema级隔离：共享数据库、独立Schema
    /// </summary>
    Schema = 2,

    /// <summary>
    /// 数据库级隔离：独立数据库
    /// </summary>
    Database = 3
}

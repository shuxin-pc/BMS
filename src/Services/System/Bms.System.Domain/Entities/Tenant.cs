using Bms.BuildingBlocks.MultiTenant.Models;
using Bms.System.Domain.Entities;

namespace Bms.System.Domain.Entities;

/// <summary>
/// 租户实体
/// </summary>
public class Tenant : BaseEntity
{
    /// <summary>
    /// 租户编码（唯一）
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 租户名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 隔离级别
    /// </summary>
    public TenantIsolationLevel IsolationLevel { get; set; } = TenantIsolationLevel.Row;

    /// <summary>
    /// 数据库连接串（数据库级隔离使用）
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// 数据库架构（Schema级隔离使用）
    /// </summary>
    public string? SchemaName { get; set; }

    /// <summary>
    /// 状态（0-禁用，1-启用）
    /// </summary>
    public int Status { get; set; } = 1;

    /// <summary>
    /// 过期时间
    /// </summary>
    public DateTime? ExpireTime { get; set; }

    /// <summary>
    /// 允许的子系统（逗号分隔）
    /// </summary>
    public string? AllowedSubsystems { get; set; }

    /// <summary>
    /// 联系人
    /// </summary>
    public string? ContactName { get; set; }

    /// <summary>
    /// 联系电话
    /// </summary>
    public string? ContactPhone { get; set; }

    /// <summary>
    /// 联系邮箱
    /// </summary>
    public string? ContactEmail { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 导航属性：租户关联的子系统
    /// </summary>
    public virtual ICollection<TenantSubsystem> TenantSubsystems { get; set; } = new List<TenantSubsystem>();
}
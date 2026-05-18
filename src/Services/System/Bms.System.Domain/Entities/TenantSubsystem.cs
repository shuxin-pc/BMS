namespace Bms.System.Domain.Entities;

/// <summary>
/// 租户子系统关联实体
/// </summary>
public class TenantSubsystem : EntityBase
{
    /// <summary>
    /// 租户ID
    /// </summary>
    public long TenantId { get; set; }

    /// <summary>
    /// 子系统ID
    /// </summary>
    public long SubsystemId { get; set; }

    /// <summary>
    /// 导航属性：租户
    /// </summary>
    public virtual Tenant? Tenant { get; set; }

    /// <summary>
    /// 导航属性：子系统
    /// </summary>
    public virtual Subsystem? Subsystem { get; set; }
}

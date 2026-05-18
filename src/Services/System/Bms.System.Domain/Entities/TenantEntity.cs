using Bms.BuildingBlocks.MultiTenant.Abstractions;

namespace Bms.System.Domain.Entities;

/// <summary>
/// 多租户实体基类
/// </summary>
public abstract class TenantEntity : BaseEntity, ITenant
{
    /// <summary>
    /// 租户ID
    /// </summary>
    public long TenantId { get; set; }

    /// <summary>
    /// 租户编码
    /// </summary>
    public string TenantCode { get; set; } = string.Empty;
}
namespace Bms.BuildingBlocks.MultiTenant.Abstractions;

/// <summary>
/// 租户实体接口
/// </summary>
public interface ITenant
{
    /// <summary>
    /// 租户ID
    /// </summary>
    long TenantId { get; set; }

    /// <summary>
    /// 租户编码
    /// </summary>
    string TenantCode { get; set; }
}

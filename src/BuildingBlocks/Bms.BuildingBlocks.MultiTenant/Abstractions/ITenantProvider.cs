using Bms.BuildingBlocks.MultiTenant.Models;

namespace Bms.BuildingBlocks.MultiTenant.Abstractions;

/// <summary>
/// 租户提供者接口
/// </summary>
public interface ITenantProvider
{
    /// <summary>
    /// 获取当前租户信息
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>租户信息</returns>
    Task<TenantInfo?> GetCurrentTenantAsync(CancellationToken cancellationToken = default);
}

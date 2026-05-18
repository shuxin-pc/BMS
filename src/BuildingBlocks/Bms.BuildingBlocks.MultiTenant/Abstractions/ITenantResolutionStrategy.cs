namespace Bms.BuildingBlocks.MultiTenant.Abstractions;

/// <summary>
/// 租户解析策略接口
/// </summary>
public interface ITenantResolutionStrategy
{
    /// <summary>
    /// 解析租户标识（租户ID或租户编码）
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>租户标识</returns>
    Task<string?> ResolveTenantIdentifierAsync(CancellationToken cancellationToken = default);
}

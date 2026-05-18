using Bms.BuildingBlocks.MultiTenant.Models;

namespace Bms.BuildingBlocks.MultiTenant.Abstractions;

/// <summary>
/// 租户存储接口
/// </summary>
public interface ITenantStore
{
    /// <summary>
    /// 根据租户ID获取租户信息
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>租户信息</returns>
    Task<TenantInfo?> GetTenantByIdAsync(long tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据租户编码获取租户信息
    /// </summary>
    /// <param name="tenantCode">租户编码</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>租户信息</returns>
    Task<TenantInfo?> GetTenantByCodeAsync(string tenantCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取所有租户信息
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>租户信息列表</returns>
    Task<IEnumerable<TenantInfo>> GetAllTenantsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新租户信息
    /// </summary>
    /// <param name="tenant">租户信息</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否更新成功</returns>
    Task<bool> UpdateTenantAsync(TenantInfo tenant, CancellationToken cancellationToken = default);

    /// <summary>
    /// 添加租户
    /// </summary>
    /// <param name="tenant">租户信息</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>新租户的ID，失败返回0</returns>
    Task<long> AddTenantAsync(TenantInfo tenant, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除租户
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否删除成功</returns>
    Task<bool> DeleteTenantAsync(long tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量删除租户
    /// </summary>
    /// <param name="tenantIds">租户ID列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>删除成功的数量</returns>
    Task<int> BatchDeleteAsync(IEnumerable<long> tenantIds, CancellationToken cancellationToken = default);
}

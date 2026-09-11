using Bms.System.Domain.Entities;

namespace Bms.System.Domain.IRepositories;

/// <summary>
/// 租户仓储接口
/// </summary>
public interface ITenantRepository
{
    Task<List<Tenant>> GetAllTenantsAsync();

    /// <summary>
    /// 获取未删除租户总数（用于统计，仅平台租户调用）
    /// </summary>
    Task<int> GetCountAsync();
}

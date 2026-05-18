using Bms.System.Domain.Entities;

namespace Bms.System.Domain.IRepositories;

/// <summary>
/// 租户仓储接口
/// </summary>
public interface ITenantRepository
{
    Task<List<Tenant>> GetAllTenantsAsync();
}

using Bms.System.Domain.Entities;

namespace Bms.System.Domain.IRepositories;

/// <summary>
/// 租户子系统关联仓储接口
/// </summary>
public interface ITenantSubsystemRepository
{
    Task<List<TenantSubsystem>> GetByTenantIdAsync(long tenantId);
    Task<List<TenantSubsystem>> GetBySubsystemIdAsync(long subsystemId);
    Task AddRangeAsync(IEnumerable<TenantSubsystem> tenantSubsystems);
    Task DeleteByTenantIdAsync(long tenantId);
    Task DeleteByTenantIdAndSubsystemIdAsync(long tenantId, long subsystemId);
    Task DeleteBySubsystemIdAsync(long subsystemId);
    Task<bool> ExistsAsync(long tenantId, long subsystemId);
}

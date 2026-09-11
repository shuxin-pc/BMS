using Bms.System.Domain.Entities;

namespace Bms.System.Domain.IRepositories;

public interface IRoleRepository
{
    Task<Role?> GetByIdAsync(long id);
    Task<Role?> GetByCodeAsync(string code);
    Task<List<Role>> GetListAsync();
    Task<List<Role>> GetPagedListAsync(int pageIndex, int pageSize);
    Task<List<Role>> GetByUserIdAsync(long userId);
    Task<List<Role>> GetByTenantIdAsync(long tenantId);

    /// <summary>
    /// 获取角色数量（用于统计）
    /// </summary>
    /// <param name="tenantId">租户ID筛选（空=全部租户）</param>
    Task<int> GetCountAsync(long? tenantId = null);
    Task<Role?> GetTenantAdminRoleAsync(long tenantId);
    Task<Role> AddAsync(Role role);
    Task UpdateAsync(Role role);
    Task DeleteAsync(long id);
    Task<bool> ExistsCodeAsync(string code, long? excludeId = null);
}
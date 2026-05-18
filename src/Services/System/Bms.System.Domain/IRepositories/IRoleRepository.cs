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
    Task<Role?> GetTenantAdminRoleAsync(long tenantId);
    Task<Role> AddAsync(Role role);
    Task UpdateAsync(Role role);
    Task DeleteAsync(long id);
    Task<bool> ExistsCodeAsync(string code, long? excludeId = null);
}
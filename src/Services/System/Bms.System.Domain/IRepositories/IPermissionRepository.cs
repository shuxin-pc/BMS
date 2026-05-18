using Bms.System.Domain.Entities;

namespace Bms.System.Domain.IRepositories;

public interface IPermissionRepository
{
    Task<Permission?> GetByIdAsync(long id);
    Task<Permission?> GetByCodeAsync(string code);
    Task<List<Permission>> GetListAsync();
    Task<List<Permission>> GetPagedListAsync(int pageIndex, int pageSize);
    Task<List<Permission>> GetByMenuIdAsync(long menuId);
    Task<List<Permission>> GetByRoleIdAsync(long roleId);
    Task<Permission> AddAsync(Permission permission);
    Task UpdateAsync(Permission permission);
    Task DeleteAsync(long id);
    Task<bool> ExistsCodeAsync(string code, long? excludeId = null);
}
using Bms.System.Domain.Entities;

namespace Bms.System.Domain.IRepositories;

public interface IDataPermissionRepository
{
    Task<DataPermission?> GetByIdAsync(long id);
    Task<DataPermission?> GetByRoleIdAsync(long roleId);
    Task<List<DataPermission>> GetListAsync();
    Task<DataPermission> AddAsync(DataPermission dataPermission);
    Task UpdateAsync(DataPermission dataPermission);
    Task DeleteAsync(long id);
    Task DeleteByRoleIdAsync(long roleId);
}
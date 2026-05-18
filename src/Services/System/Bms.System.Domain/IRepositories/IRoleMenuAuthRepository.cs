using Bms.System.Domain.Entities;

namespace Bms.System.Domain.IRepositories;

/// <summary>
/// 角色菜单权限仓储接口
/// </summary>
public interface IRoleMenuAuthRepository
{
    Task<List<RoleMenuAuth>> GetByRoleIdAsync(long roleId);
    Task<List<RoleMenuAuth>> GetByRoleIdsAsync(IEnumerable<long> roleIds);
    Task<List<RoleMenuAuth>> GetByMenuIdAsync(long menuId);
    Task<List<RoleMenuAuth>> GetByMenuIdsAsync(IEnumerable<long> menuIds);
    Task<List<RoleMenuAuth>> GetByRoleIdAndSubsystemIdAsync(long roleId, long subsystemId);
    Task<List<RoleMenuAuth>> GetBySubsystemIdAsync(long subsystemId);
    Task AddRangeAsync(IEnumerable<RoleMenuAuth> roleMenuAuths);
    Task DeleteByRoleIdAsync(long roleId);
    Task DeleteByRoleIdAndMenuIdAsync(long roleId, long menuId);
    Task DeleteByRoleIdAndMenuIdsAsync(long roleId, IEnumerable<long> menuIds);
    Task DeleteByMenuIdAsync(long menuId);
    Task DeleteBySubsystemIdAsync(long subsystemId);
    Task UpdateSubsystemIdByMenuIdAsync(long menuId, long newSubsystemId);
}

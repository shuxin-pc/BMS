using Bms.System.Domain.Entities;

namespace Bms.System.Domain.IRepositories;

public interface IOrganizationRepository
{
    Task<Organization?> GetByIdAsync(long id);
    Task<Organization?> GetByCodeAsync(string code);
    Task<List<Organization>> GetListAsync();
    Task<List<Organization>> GetAllTreeAsync();
    Task<List<Organization>> GetChildrenAsync(long? parentId);
    Task<List<Organization>> GetByUserIdAsync(long userId);
    /// <summary>
    /// 获取指定租户下的所有组织（用于 All 数据权限填充组织列表）
    /// </summary>
    Task<List<Organization>> GetByTenantIdAsync(long tenantId);
    Task<bool> HasUsersAsync(long organizationId);
    /// <summary>
    /// 获取指定组织的所有子孙组织 ID（递归，包含多级子孙）。
    /// 用于组织移动时的环路检查：新父组织不能是当前组织的子孙，否则会形成环路导致递归查询栈溢出。
    /// </summary>
    Task<List<long>> GetAllChildIdsAsync(long id);
    Task<Organization> AddAsync(Organization organization);
    Task UpdateAsync(Organization organization);
    Task DeleteAsync(long id);
    Task<bool> ExistsCodeAsync(string code, long? excludeId = null);
}
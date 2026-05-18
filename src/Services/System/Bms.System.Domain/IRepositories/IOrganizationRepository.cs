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
    Task<bool> HasUsersAsync(long organizationId);
    Task<Organization> AddAsync(Organization organization);
    Task UpdateAsync(Organization organization);
    Task DeleteAsync(long id);
    Task<bool> ExistsCodeAsync(string code, long? excludeId = null);
}
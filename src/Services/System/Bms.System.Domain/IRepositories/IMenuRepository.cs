using Bms.System.Domain.Entities;

namespace Bms.System.Domain.IRepositories;

public interface IMenuRepository
{
    Task<Menu?> GetByIdAsync(long id);
    Task<Menu?> GetByCodeAsync(string code);
    Task<List<Menu>> GetListAsync();
    Task<List<Menu>> GetAllMenuTreeAsync();
    Task<List<Menu>> GetByUserIdAsync(long userId);
    Task<List<Menu>> GetByRoleIdAsync(long roleId);
    Task<Menu> AddAsync(Menu menu);
    Task UpdateAsync(Menu menu);
    Task DeleteAsync(long id);
    Task<bool> ExistsCodeAsync(string code, long? excludeId = null);
}
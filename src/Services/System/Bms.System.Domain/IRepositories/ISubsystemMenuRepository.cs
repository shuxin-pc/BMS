using Bms.System.Domain.Entities;

namespace Bms.System.Domain.IRepositories;

/// <summary>
/// 子系统菜单关联仓储接口
/// </summary>
public interface ISubsystemMenuRepository
{
    Task<List<SubsystemMenu>> GetBySubsystemIdAsync(long subsystemId);
    Task<List<SubsystemMenu>> GetByMenuIdsAsync(IEnumerable<long> menuIds);
    Task<List<SubsystemMenu>> GetByMenuIdAsync(long menuId);
    Task AddRangeAsync(IEnumerable<SubsystemMenu> subsystemMenus);
    Task DeleteBySubsystemIdAsync(long subsystemId);
    Task DeleteByMenuIdAsync(long menuId);
    Task DeleteBySubsystemIdAndMenuIdsAsync(long subsystemId, IEnumerable<long> menuIds);
    Task ReplaceAllAsync(long subsystemId, IEnumerable<long> menuIds);
}

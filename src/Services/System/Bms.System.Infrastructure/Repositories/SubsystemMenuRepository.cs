using Microsoft.EntityFrameworkCore;
using Bms.System.Domain.Entities;
using Bms.System.Domain.IRepositories;

namespace Bms.System.Infrastructure.Repositories;

/// <summary>
/// 子系统菜单关联仓储实现
/// </summary>
public class SubsystemMenuRepository : ISubsystemMenuRepository
{
    private readonly SystemDbContext _context;

    public SubsystemMenuRepository(SystemDbContext context)
    {
        _context = context;
    }

    public async Task<List<SubsystemMenu>> GetBySubsystemIdAsync(long subsystemId)
    {
        return await _context.SubsystemMenus
            .Include(sm => sm.Menu)
            .Where(sm => sm.SubsystemId == subsystemId)
            .ToListAsync();
    }

    public async Task<List<SubsystemMenu>> GetByMenuIdsAsync(IEnumerable<long> menuIds)
    {
        return await _context.SubsystemMenus
            .Where(sm => menuIds.Contains(sm.MenuId))
            .ToListAsync();
    }

    public async Task<List<SubsystemMenu>> GetByMenuIdAsync(long menuId)
    {
        return await _context.SubsystemMenus
            .Where(sm => sm.MenuId == menuId)
            .ToListAsync();
    }

    public async Task AddRangeAsync(IEnumerable<SubsystemMenu> subsystemMenus)
    {
        await _context.SubsystemMenus.AddRangeAsync(subsystemMenus);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteBySubsystemIdAsync(long subsystemId)
    {
        var items = await _context.SubsystemMenus
            .Where(sm => sm.SubsystemId == subsystemId)
            .ToListAsync();
        _context.SubsystemMenus.RemoveRange(items);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// 替换子系统的所有菜单（先删后增）
    /// </summary>
    public async Task ReplaceAllAsync(long subsystemId, IEnumerable<long> menuIds)
    {
        var menuIdList = menuIds.ToList();

        // 先物理删除旧数据
        var items = await _context.SubsystemMenus
            .Where(sm => sm.SubsystemId == subsystemId)
            .ToListAsync();
        _context.SubsystemMenus.RemoveRange(items);
        await _context.SaveChangesAsync();

        // 添加新数据
        if (menuIdList.Count > 0)
        {
            foreach (var menuId in menuIdList)
            {
                _context.SubsystemMenus.Add(new SubsystemMenu
                {
                    SubsystemId = subsystemId,
                    MenuId = menuId,
                    CreatedTime = DateTime.UtcNow,
                    UpdatedTime = DateTime.UtcNow
                });
            }
            await _context.SaveChangesAsync();
        }
    }

    public async Task DeleteByMenuIdAsync(long menuId)
    {
        var items = await _context.SubsystemMenus
            .Where(sm => sm.MenuId == menuId)
            .ToListAsync();
        _context.SubsystemMenus.RemoveRange(items);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteBySubsystemIdAndMenuIdsAsync(long subsystemId, IEnumerable<long> menuIds)
    {
        var items = await _context.SubsystemMenus
            .Where(sm => sm.SubsystemId == subsystemId && menuIds.Contains(sm.MenuId))
            .ToListAsync();
        _context.SubsystemMenus.RemoveRange(items);
        await _context.SaveChangesAsync();
    }
}
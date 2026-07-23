using Microsoft.EntityFrameworkCore;
using Bms.System.Domain.Entities;
using Bms.System.Domain.IRepositories;

namespace Bms.System.Infrastructure.Repositories;

/// <summary>
/// 角色菜单权限仓储实现
/// </summary>
public class RoleMenuAuthRepository : IRoleMenuAuthRepository
{
    private readonly SystemDbContext _context;

    public RoleMenuAuthRepository(SystemDbContext context)
    {
        _context = context;
    }

    public async Task<List<RoleMenuAuth>> GetByRoleIdAsync(long roleId)
    {
        return await _context.RoleMenuAuths
            .Include(rma => rma.Menu)
            .Where(rma => rma.RoleId == roleId)
            .ToListAsync();
    }

    public async Task<List<RoleMenuAuth>> GetByRoleIdsAsync(IEnumerable<long> roleIds)
    {
        return await _context.RoleMenuAuths
            .Include(rma => rma.Menu)
            .Where(rma => roleIds.Contains(rma.RoleId))
            .ToListAsync();
    }

    public async Task<List<RoleMenuAuth>> GetByMenuIdAsync(long menuId)
    {
        return await _context.RoleMenuAuths
            .Where(rma => rma.MenuId == menuId)
            .ToListAsync();
    }

    public async Task<List<RoleMenuAuth>> GetByMenuIdsAsync(IEnumerable<long> menuIds)
    {
        return await _context.RoleMenuAuths
            .Where(rma => menuIds.Contains(rma.MenuId))
            .ToListAsync();
    }

    public async Task<List<RoleMenuAuth>> GetByRoleIdAndSubsystemIdAsync(long roleId, long subsystemId)
    {
        return await _context.RoleMenuAuths
            .Include(rma => rma.Menu)
            .Where(rma => rma.RoleId == roleId && rma.SubsystemId == subsystemId)
            .ToListAsync();
    }

    public async Task<List<RoleMenuAuth>> GetBySubsystemIdAsync(long subsystemId)
    {
        return await _context.RoleMenuAuths
            .Where(rma => rma.SubsystemId == subsystemId)
            .ToListAsync();
    }

    public async Task AddRangeAsync(IEnumerable<RoleMenuAuth> roleMenuAuths)
    {
        await _context.RoleMenuAuths.AddRangeAsync(roleMenuAuths);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteByRoleIdAsync(long roleId)
    {
        var items = await _context.RoleMenuAuths
            .Where(rma => rma.RoleId == roleId)
            .ToListAsync();
        _context.RoleMenuAuths.RemoveRange(items);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteByRoleIdAndMenuIdAsync(long roleId, long menuId)
    {
        var item = await _context.RoleMenuAuths
            .FirstOrDefaultAsync(rma => rma.RoleId == roleId && rma.MenuId == menuId);
        if (item != null)
        {
            _context.RoleMenuAuths.Remove(item);
            await _context.SaveChangesAsync();
        }
    }

    public async Task DeleteByRoleIdAndMenuIdsAsync(long roleId, IEnumerable<long> menuIds)
    {
        var items = await _context.RoleMenuAuths
            .Where(rma => rma.RoleId == roleId && menuIds.Contains(rma.MenuId))
            .ToListAsync();
        _context.RoleMenuAuths.RemoveRange(items);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteByMenuIdAsync(long menuId)
    {
        var items = await _context.RoleMenuAuths
            .Where(rma => rma.MenuId == menuId)
            .ToListAsync();
        _context.RoleMenuAuths.RemoveRange(items);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteBySubsystemIdAsync(long subsystemId)
    {
        var items = await _context.RoleMenuAuths
            .Where(rma => rma.SubsystemId == subsystemId)
            .ToListAsync();
        _context.RoleMenuAuths.RemoveRange(items);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateSubsystemIdByMenuIdAsync(long menuId, long newSubsystemId)
    {
        var items = await _context.RoleMenuAuths
            .Where(rma => rma.MenuId == menuId)
            .ToListAsync();
        foreach (var item in items)
        {
            item.SubsystemId = newSubsystemId;
            item.UpdatedTime = DateTime.Now;
        }
        await _context.SaveChangesAsync();
    }
}
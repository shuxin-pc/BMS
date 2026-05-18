using Microsoft.EntityFrameworkCore;
using Bms.System.Domain.Entities;
using Bms.System.Domain.IRepositories;

namespace Bms.System.Infrastructure.Repositories;

public class MenuRepository : IMenuRepository
{
    private readonly SystemDbContext _context;

    public MenuRepository(SystemDbContext context)
    {
        _context = context;
    }

    public async Task<Menu?> GetByIdAsync(long id)
    {
        return await _context.Menus
            .Include(m => m.Children)
            .Include(m => m.Permissions)
            .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);
    }

    public async Task<Menu?> GetByCodeAsync(string code)
    {
        return await _context.Menus
            .Include(m => m.Children)
            .Include(m => m.Permissions)
            .FirstOrDefaultAsync(m => m.Code == code && !m.IsDeleted);
    }

    public async Task<List<Menu>> GetListAsync()
    {
        return await _context.Menus
            .Where(m => !m.IsDeleted)
            .OrderBy(m => m.Sort)
            .ToListAsync();
    }

    public async Task<List<Menu>> GetAllMenuTreeAsync()
    {
        var allMenus = await _context.Menus
            .Where(m => !m.IsDeleted)
            .OrderBy(m => m.Sort)
            .ToListAsync();

        return BuildMenuTree(allMenus, null);
    }

    public async Task<List<Menu>> GetByUserIdAsync(long userId)
    {
        var roleIds = await _context.UserRoles
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.RoleId)
            .ToListAsync();

        var menuIds = await _context.RolePermissions
            .Where(rp => roleIds.Contains(rp.RoleId))
            .Select(rp => rp.Permission.MenuId)
            .Distinct()
            .ToListAsync();

        var menus = await _context.Menus
            .Where(m => !m.IsDeleted && m.Status == 1 && (menuIds.Contains(m.Id) || m.Type == 0))
            .OrderBy(m => m.Sort)
            .ToListAsync();

        return menus;
    }

    public async Task<List<Menu>> GetByRoleIdAsync(long roleId)
    {
        var menuIds = await _context.RolePermissions
            .Where(rp => rp.RoleId == roleId)
            .Select(rp => rp.Permission.MenuId)
            .Distinct()
            .ToListAsync();

        return await _context.Menus
            .Where(m => !m.IsDeleted && menuIds.Contains(m.Id))
            .OrderBy(m => m.Sort)
            .ToListAsync();
    }

    public async Task<Menu> AddAsync(Menu menu)
    {
        await _context.Menus.AddAsync(menu);
        await _context.SaveChangesAsync();
        return menu;
    }

    public async Task UpdateAsync(Menu menu)
    {
        menu.UpdatedTime = DateTime.UtcNow;
        _context.Menus.Update(menu);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(long id)
    {
        var menu = await _context.Menus.FindAsync(id);
        if (menu != null)
        {
            menu.IsDeleted = true;
            menu.UpdatedTime = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsCodeAsync(string code, long? excludeId = null)
    {
        return await _context.Menus
            .AnyAsync(m => m.Code == code && !m.IsDeleted && (excludeId == null || m.Id != excludeId));
    }

    private List<Menu> BuildMenuTree(List<Menu> allMenus, long? parentId)
    {
        return allMenus
            .Where(m => m.ParentId == parentId)
            .Select(m =>
            {
                m.Children = BuildMenuTree(allMenus, m.Id);
                return m;
            })
            .ToList();
    }
}
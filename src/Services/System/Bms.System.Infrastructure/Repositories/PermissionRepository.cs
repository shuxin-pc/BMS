using Microsoft.EntityFrameworkCore;
using Bms.System.Domain.Entities;
using Bms.System.Domain.IRepositories;

namespace Bms.System.Infrastructure.Repositories;

public class PermissionRepository : IPermissionRepository
{
    private readonly SystemDbContext _context;

    public PermissionRepository(SystemDbContext context)
    {
        _context = context;
    }

    public async Task<Permission?> GetByIdAsync(long id)
    {
        return await _context.Permissions
            .Include(p => p.Menu)
            .Include(p => p.RolePermissions)
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
    }

    public async Task<Permission?> GetByCodeAsync(string code)
    {
        return await _context.Permissions
            .FirstOrDefaultAsync(p => p.Code == code && !p.IsDeleted);
    }

    public async Task<List<Permission>> GetListAsync()
    {
        return await _context.Permissions
            .Where(p => !p.IsDeleted)
            .Include(p => p.Menu)
            .OrderBy(p => p.CreatedTime)
            .ToListAsync();
    }

    public async Task<List<Permission>> GetPagedListAsync(int pageIndex, int pageSize)
    {
        return await _context.Permissions
            .Where(p => !p.IsDeleted)
            .Include(p => p.Menu)
            .OrderBy(p => p.CreatedTime)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<List<Permission>> GetByMenuIdAsync(long menuId)
    {
        return await _context.Permissions
            .Where(p => p.MenuId == menuId && !p.IsDeleted)
            .ToListAsync();
    }

    public async Task<List<Permission>> GetByRoleIdAsync(long roleId)
    {
        return await _context.Permissions
            .Where(p => !p.IsDeleted)
            .Join(_context.RolePermissions.Where(rp => rp.RoleId == roleId),
                p => p.Id,
                rp => rp.PermissionId,
                (p, rp) => p)
            .Distinct()
            .ToListAsync();
    }

    public async Task<Permission> AddAsync(Permission permission)
    {
        await _context.Permissions.AddAsync(permission);
        await _context.SaveChangesAsync();
        return permission;
    }

    public async Task UpdateAsync(Permission permission)
    {
        permission.UpdatedTime = DateTime.Now;
        _context.Permissions.Update(permission);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(long id)
    {
        var permission = await _context.Permissions.FindAsync(id);
        if (permission != null)
        {
            permission.IsDeleted = true;
            permission.UpdatedTime = DateTime.Now;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsCodeAsync(string code, long? excludeId = null)
    {
        return await _context.Permissions
            .AnyAsync(p => p.Code == code && !p.IsDeleted && (excludeId == null || p.Id != excludeId));
    }
}
using Microsoft.EntityFrameworkCore;
using Bms.System.Domain.Entities;
using Bms.System.Domain.IRepositories;

namespace Bms.System.Infrastructure.Repositories;

public class RoleRepository : IRoleRepository
{
    private readonly SystemDbContext _context;

    public RoleRepository(SystemDbContext context)
    {
        _context = context;
    }

    public async Task<Role?> GetByIdAsync(long id)
    {
        return await _context.Roles
            .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
            .Include(r => r.UserRoles)
            .Include(r => r.DataPermission)
            .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);
    }

    public async Task<Role?> GetByCodeAsync(string code)
    {
        return await _context.Roles
            .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(r => r.Code == code && !r.IsDeleted);
    }

    public async Task<List<Role>> GetListAsync()
    {
        return await _context.Roles
            .Include(r => r.DataPermission)
            .Where(r => !r.IsDeleted)
            .OrderBy(r => r.Sort)
            .ToListAsync();
    }

    public async Task<List<Role>> GetPagedListAsync(int pageIndex, int pageSize)
    {
        return await _context.Roles
            .Include(r => r.DataPermission)
            .Where(r => !r.IsDeleted)
            .OrderBy(r => r.Sort)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<List<Role>> GetByUserIdAsync(long userId)
    {
        return await _context.Roles
            .Where(r => !r.IsDeleted)
            .Join(_context.UserRoles.Where(ur => ur.UserId == userId),
                r => r.Id,
                ur => ur.RoleId,
                (r, ur) => r)
            .ToListAsync();
    }

    public async Task<List<Role>> GetByTenantIdAsync(long tenantId)
    {
        return await _context.Roles
            .Where(r => !r.IsDeleted && r.TenantId == tenantId)
            .ToListAsync();
    }

    public async Task<Role?> GetTenantAdminRoleAsync(long tenantId)
    {
        return await _context.Roles
            .FirstOrDefaultAsync(r => r.Code == "tenant_admin" && !r.IsDeleted);
    }

    public async Task<Role> AddAsync(Role role)
    {
        await _context.Roles.AddAsync(role);
        await _context.SaveChangesAsync();
        return role;
    }

    public async Task UpdateAsync(Role role)
    {
        role.UpdatedTime = DateTime.UtcNow;

        // 使用 Attach 并排除租户ID属性，防止触发租户ID修改验证
        var entry = _context.Roles.Attach(role);
        entry.State = EntityState.Modified;

        // 排除租户相关字段被标记为修改
        entry.Property(e => e.TenantId).IsModified = false;
        entry.Property(e => e.TenantCode).IsModified = false;

        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(long id)
    {
        var role = await _context.Roles.FindAsync(id);
        if (role != null)
        {
            if (role.IsSystem)
            {
                throw new InvalidOperationException("系统角色不能删除");
            }
            role.IsDeleted = true;
            role.UpdatedTime = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsCodeAsync(string code, long? excludeId = null)
    {
        return await _context.Roles
            .AnyAsync(r => r.Code == code && !r.IsDeleted && (excludeId == null || r.Id != excludeId));
    }
}

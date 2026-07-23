using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Bms.System.Domain.Entities;
using Bms.System.Domain.IRepositories;

namespace Bms.System.Infrastructure.Repositories;

public class RoleRepository : IRoleRepository
{
    private readonly SystemDbContext _context;
    private readonly ILogger<RoleRepository> _logger;

    public RoleRepository(SystemDbContext context, ILogger<RoleRepository> logger)
    {
        _context = context;
        _logger = logger;
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
            .OrderByDescending(r => r.CreatedTime)
            .ToListAsync();
    }

    public async Task<List<Role>> GetPagedListAsync(int pageIndex, int pageSize)
    {
        return await _context.Roles
            .Include(r => r.DataPermission)
            .Where(r => !r.IsDeleted)
            .OrderByDescending(r => r.CreatedTime)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<List<Role>> GetByUserIdAsync(long userId)
    {
        // Include RolePermissions.Permission，否则权限中间件读取的权限列表为空
        var roles = await _context.Roles
            .Where(r => !r.IsDeleted && r.UserRoles.Any(ur => ur.UserId == userId))
            .Include(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            .ToListAsync();

        // 诊断：逐表查证该用户每个角色的权限数据分布
        foreach (var r in roles)
        {
            var rpCount = await _context.RolePermissions.CountAsync(rp => rp.RoleId == r.Id);
            var rmaCount = await _context.RoleMenuAuths.CountAsync(rma => rma.RoleId == r.Id);
            var loadedRpCount = r.RolePermissions?.Count ?? 0;
            _logger.LogWarning(
                "【权限调试】GetByUserIdAsync RoleId={RoleId}, Code={Code}, DB.RolePermissions={RpInDb}, 已加载RolePermissions={Loaded}, DB.RoleMenuAuths={RmaInDb}",
                r.Id, r.Code, rpCount, loadedRpCount, rmaCount);
        }

        return roles;
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
        role.UpdatedTime = DateTime.Now;

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
            role.UpdatedTime = DateTime.Now;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsCodeAsync(string code, long? excludeId = null)
    {
        return await _context.Roles
            .AnyAsync(r => r.Code == code && !r.IsDeleted && (excludeId == null || r.Id != excludeId));
    }
}

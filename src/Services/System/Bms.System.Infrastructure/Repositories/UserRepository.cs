using Microsoft.EntityFrameworkCore;
using Bms.System.Domain.Entities;
using Bms.System.Domain.IRepositories;

namespace Bms.System.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly SystemDbContext _context;

    public UserRepository(SystemDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(long id)
    {
        return await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .Include(u => u.Organization)
            .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);
    }

    public async Task<User?> GetByUserNameAsync(string userName)
    {
        return await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .Include(u => u.Organization)
            .FirstOrDefaultAsync(u => u.UserName == userName && !u.IsDeleted);
    }

    public async Task<User?> GetByPhoneAsync(string phone)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Phone == phone && !u.IsDeleted);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email && !u.IsDeleted);
    }

    public async Task<List<User>> GetListAsync(long? tenantId = null, long? userId = null, List<long>? organizationIds = null, long? creatorTenantId = null)
    {
        var query = _context.Users.Where(u => !u.IsDeleted);

        // 按租户ID筛选
        if (tenantId.HasValue)
        {
            query = query.Where(u => u.TenantId == tenantId.Value);
        }

        // 按用户ID筛选（仅本人模式，数据权限 ScopeType=Self）
        if (userId.HasValue)
        {
            query = query.Where(u => u.Id == userId.Value);
        }

        // 按组织ID列表筛选（部门及以下/自定义模式，数据权限 ScopeType=DepartmentAndBelow/Custom）
        if (organizationIds != null && organizationIds.Any())
        {
            query = query.Where(u => u.OrganizationId.HasValue && organizationIds.Contains(u.OrganizationId.Value));
        }

        // 按创建者租户ID筛选（屏蔽平台跨租户创建的用户，如 tenant_admin）
        if (creatorTenantId.HasValue)
        {
            query = query.Where(u => u.CreatorTenantId == creatorTenantId.Value);
        }

        return await query
            .Include(u => u.Organization)
            .OrderBy(u => u.CreatedTime)
            .ToListAsync();
    }

    public async Task<List<User>> GetPagedListAsync(int pageIndex, int pageSize, string? userName = null, string? realName = null, int? status = null, long? tenantId = null, long? organizationId = null, long? userId = null, List<long>? organizationIds = null, long? roleId = null, long? creatorTenantId = null)
    {
        var query = _context.Users.Where(u => !u.IsDeleted);

        // 按用户名模糊搜索（忽略大小写）
        if (!string.IsNullOrEmpty(userName))
        {
            query = query.Where(u => u.UserName != null && u.UserName.ToLower().Contains(userName.ToLower()));
        }

        // 按真实姓名模糊搜索（忽略大小写）
        if (!string.IsNullOrEmpty(realName))
        {
            query = query.Where(u => u.RealName != null && u.RealName.ToLower().Contains(realName.ToLower()));
        }

        // 按状态筛选
        if (status.HasValue)
        {
            query = query.Where(u => u.Status == status.Value);
        }

        // 按租户ID筛选
        if (tenantId.HasValue)
        {
            query = query.Where(u => u.TenantId == tenantId.Value);
        }

        // 按组织ID筛选：organizationIds 优先（包含子组织），否则用单个 organizationId
        if (organizationIds != null && organizationIds.Any())
        {
            // 部门及以下/自定义模式：筛选属于组织列表中的任何组织的用户
            query = query.Where(u => u.OrganizationId.HasValue && organizationIds.Contains(u.OrganizationId.Value));
        }
        else if (organizationId.HasValue)
        {
            // 单个组织模式：精确匹配
            query = query.Where(u => u.OrganizationId == organizationId.Value);
        }

        // 按用户ID筛选（仅本人模式）
        if (userId.HasValue)
        {
            query = query.Where(u => u.Id == userId.Value);
        }

        // 按角色ID筛选
        if (roleId.HasValue)
        {
            query = query.Where(u => u.UserRoles.Any(ur => ur.RoleId == roleId.Value));
        }

        // 按创建者租户ID筛选（屏蔽平台跨租户创建的用户）
        if (creatorTenantId.HasValue)
        {
            query = query.Where(u => u.CreatorTenantId == creatorTenantId.Value);
        }

        return await query
            .Include(u => u.Organization)
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .OrderByDescending(u => u.CreatedTime)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<int> GetCountAsync(string? userName = null, string? realName = null, int? status = null, long? tenantId = null, long? organizationId = null, long? userId = null, List<long>? organizationIds = null, long? roleId = null, long? creatorTenantId = null)
    {
        var query = _context.Users.Where(u => !u.IsDeleted);

        // 按用户名模糊搜索（忽略大小写）
        if (!string.IsNullOrEmpty(userName))
        {
            query = query.Where(u => u.UserName != null && u.UserName.ToLower().Contains(userName.ToLower()));
        }

        // 按真实姓名模糊搜索（忽略大小写）
        if (!string.IsNullOrEmpty(realName))
        {
            query = query.Where(u => u.RealName != null && u.RealName.ToLower().Contains(realName.ToLower()));
        }

        // 按状态筛选
        if (status.HasValue)
        {
            query = query.Where(u => u.Status == status.Value);
        }

        // 按租户ID筛选
        if (tenantId.HasValue)
        {
            query = query.Where(u => u.TenantId == tenantId.Value);
        }

        // 按组织ID筛选：organizationIds 优先（包含子组织），否则用单个 organizationId
        if (organizationIds != null && organizationIds.Any())
        {
            // 部门及以下/自定义模式：筛选属于组织列表中的任何组织的用户
            query = query.Where(u => u.OrganizationId.HasValue && organizationIds.Contains(u.OrganizationId.Value));
        }
        else if (organizationId.HasValue)
        {
            // 单个组织模式：精确匹配
            query = query.Where(u => u.OrganizationId == organizationId.Value);
        }

        // 按用户ID筛选（仅本人模式）
        if (userId.HasValue)
        {
            query = query.Where(u => u.Id == userId.Value);
        }

        // 按角色ID筛选
        if (roleId.HasValue)
        {
            query = query.Where(u => u.UserRoles.Any(ur => ur.RoleId == roleId.Value));
        }

        // 按创建者租户ID筛选（屏蔽平台跨租户创建的用户）
        if (creatorTenantId.HasValue)
        {
            query = query.Where(u => u.CreatorTenantId == creatorTenantId.Value);
        }

        return await query.CountAsync();
    }

    public async Task<User> AddAsync(User user)
    {
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task UpdateAsync(User user)
    {
        user.UpdatedTime = DateTime.Now;
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(long id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user != null)
        {
            user.IsDeleted = true;
            user.UpdatedTime = DateTime.Now;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsUserNameAsync(string userName, long? excludeId = null)
    {
        return await _context.Users
            .AnyAsync(u => u.UserName == userName && !u.IsDeleted && (excludeId == null || u.Id != excludeId));
    }

    public async Task<bool> ExistsPhoneAsync(string phone, long? excludeId = null)
    {
        if (string.IsNullOrEmpty(phone)) return false;
        return await _context.Users
            .AnyAsync(u => u.Phone == phone && !u.IsDeleted && (excludeId == null || u.Id != excludeId));
    }

    public async Task<bool> ExistsEmailAsync(string email, long? excludeId = null)
    {
        if (string.IsNullOrEmpty(email)) return false;
        return await _context.Users
            .AnyAsync(u => u.Email == email && !u.IsDeleted && (excludeId == null || u.Id != excludeId));
    }

    public async Task<List<Role>> GetUserRolesAsync(long userId)
    {
        var roles = await _context.UserRoles
            .Where(ur => ur.UserId == userId)
            .Include(ur => ur.Role)
            .Where(ur => ur.Role != null && !ur.Role.IsDeleted)
            .Select(ur => ur.Role)
            .ToListAsync();

        return roles.OfType<Role>().ToList();
    }

    public async Task<List<string>> GetUserPermissionsAsync(long userId)
    {
        // 用户所属租户已授权的子系统ID：角色可能跨租户共用（如全局 tenant_admin 挂到各租户用户），
        // 权限码必须落在租户开通范围内，否则取消租户子系统授权后角色残留授权仍会越权生效
        var userTenantId = await _context.Users
            .Where(u => u.Id == userId)
            .Select(u => u.TenantId)
            .FirstOrDefaultAsync();
        var authorizedSubsystemIds = await _context.TenantSubsystems
            .Where(ts => ts.TenantId == userTenantId)
            .Select(ts => ts.SubsystemId)
            .ToListAsync();

        var roleIds = await _context.UserRoles
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.RoleId)
            .ToListAsync();

        // 获取角色授权的按钮菜单权限码（从 RoleMenuAuths -> Menu.Code）
        var buttonMenuIds = await _context.RoleMenuAuths
            .Where(rma => roleIds.Contains(rma.RoleId))
            .Select(rma => rma.MenuId)
            .Distinct()
            .ToListAsync();

        // 权限码 = 角色按钮权限 ∩ 租户已授权子系统（按钮菜单经 SubsystemMenus 归属子系统）
        var permissionCodes = await _context.Menus
            .Where(m => buttonMenuIds.Contains(m.Id) && m.Type == 2 && !m.IsDeleted)
            .Join(_context.SubsystemMenus,
                m => m.Id,
                sm => sm.MenuId,
                (m, sm) => new { m.Code, sm.SubsystemId })
            .Where(x => !string.IsNullOrEmpty(x.Code) && authorizedSubsystemIds.Contains(x.SubsystemId))
            .Select(x => x.Code)
            .Distinct()
            .ToListAsync();

        return permissionCodes;
    }
}
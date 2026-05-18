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

    public async Task<List<User>> GetListAsync(long? tenantId = null)
    {
        var query = _context.Users.Where(u => !u.IsDeleted);

        if (tenantId.HasValue)
        {
            query = query.Where(u => u.TenantId == tenantId.Value);
        }

        return await query
            .Include(u => u.Organization)
            .OrderBy(u => u.CreatedTime)
            .ToListAsync();
    }

    public async Task<List<User>> GetPagedListAsync(int pageIndex, int pageSize, string? userName = null, string? realName = null, int? status = null, long? tenantId = null, long? organizationId = null, long? userId = null, List<long>? organizationIds = null, long? roleId = null)
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

        return await query
            .Include(u => u.Organization)
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .OrderByDescending(u => u.CreatedTime)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<int> GetCountAsync(string? userName = null, string? realName = null, int? status = null, long? tenantId = null, long? organizationId = null, long? userId = null, List<long>? organizationIds = null, long? roleId = null)
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
        user.UpdatedTime = DateTime.UtcNow;
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(long id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user != null)
        {
            user.IsDeleted = true;
            user.UpdatedTime = DateTime.UtcNow;
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

    public async Task<List<Permission>> GetUserPermissionsAsync(long userId)
    {
        var roleIds = await _context.UserRoles
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.RoleId)
            .ToListAsync();

        // 1. 获取角色对应的API权限（Permission表）
        var permissions = await _context.RolePermissions
            .Where(rp => roleIds.Contains(rp.RoleId))
            .Include(rp => rp.Permission)
            .Where(rp => rp.Permission != null && !rp.Permission.IsDeleted)
            .Select(rp => rp.Permission)
            .ToListAsync();

        var result = permissions.OfType<Permission>().ToList();

        // 2. 获取角色授权的按钮菜单权限（从RoleMenuAuth获取type=2的菜单）
        var buttonMenuIds = await _context.RoleMenuAuths
            .Where(rma => roleIds.Contains(rma.RoleId))
            .Select(rma => rma.MenuId)
            .Distinct()
            .ToListAsync();

        var buttonMenus = await _context.Menus
            .Where(m => buttonMenuIds.Contains(m.Id) && m.Type == 2 && !m.IsDeleted)
            .ToListAsync();

        // 将按钮菜单的权限码转为Permission对象加入结果
        foreach (var menu in buttonMenus)
        {
            // 按钮类型：Code 和 PermissionCode 已统一，直接使用 Code 即可
            if (!string.IsNullOrEmpty(menu.Code) && !result.Any(p => p.Code == menu.Code))
            {
                result.Add(new Permission
                {
                    Id = menu.Id,
                    Code = menu.Code,
                    Name = menu.Name,
                    MenuId = menu.Id,
                    IsDeleted = false
                });
            }
        }

        return result;
    }
}
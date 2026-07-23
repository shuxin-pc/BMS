using Microsoft.EntityFrameworkCore;
using Bms.System.Domain.Entities;
using Bms.System.Domain.IRepositories;

namespace Bms.System.Infrastructure.Repositories;

public class OrganizationRepository : IOrganizationRepository
{
    private readonly SystemDbContext _context;

    public OrganizationRepository(SystemDbContext context)
    {
        _context = context;
    }

    public async Task<Organization?> GetByIdAsync(long id)
    {
        return await _context.Organizations
            .Include(o => o.Children)
            .Include(o => o.Users)
            .FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted);
    }

    public async Task<Organization?> GetByCodeAsync(string code)
    {
        return await _context.Organizations
            .FirstOrDefaultAsync(o => o.Code == code && !o.IsDeleted);
    }

    public async Task<List<Organization>> GetListAsync()
    {
        return await _context.Organizations
            .Where(o => !o.IsDeleted)
            .OrderBy(o => o.Sort)
            .ToListAsync();
    }

    public async Task<List<Organization>> GetAllTreeAsync()
    {
        var allOrgs = await _context.Organizations
            .Where(o => !o.IsDeleted)
            .OrderBy(o => o.Sort)
            .ToListAsync();

        return BuildOrgTree(allOrgs, null);
    }

    public async Task<List<Organization>> GetChildrenAsync(long? parentId)
    {
        return await _context.Organizations
            .Where(o => o.ParentId == parentId && !o.IsDeleted)
            .OrderBy(o => o.Sort)
            .ToListAsync();
    }

    public async Task<bool> HasUsersAsync(long organizationId)
    {
        return await _context.Users
            .AnyAsync(u => u.OrganizationId == organizationId && !u.IsDeleted);
    }

    /// <summary>
    /// 获取指定租户下的所有组织（用于 All 数据权限填充组织列表）
    /// </summary>
    public async Task<List<Organization>> GetByTenantIdAsync(long tenantId)
    {
        return await _context.Organizations
            .Where(o => o.TenantId == tenantId && !o.IsDeleted)
            .ToListAsync();
    }

    public async Task<List<Organization>> GetByUserIdAsync(long userId)
    {
        var user = await _context.Users
            .Include(u => u.Organization)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user?.OrganizationId == null)
            return new List<Organization>();

        var orgIds = new List<long> { user.OrganizationId.Value };
        // 获取所有父级组织
        var currentOrg = await GetByIdAsync(user.OrganizationId.Value);
        while (currentOrg != null && currentOrg.ParentId != null)
        {
            orgIds.Add(currentOrg.ParentId.Value);
            currentOrg = await GetByIdAsync(currentOrg.ParentId.Value);
        }

        return await _context.Organizations
            .Where(o => orgIds.Contains(o.Id) && !o.IsDeleted)
            .ToListAsync();
    }

    public async Task<Organization> AddAsync(Organization organization)
    {
        await _context.Organizations.AddAsync(organization);
        await _context.SaveChangesAsync();
        return organization;
    }

    public async Task UpdateAsync(Organization organization)
    {
        organization.UpdatedTime = DateTime.Now;
        _context.Organizations.Update(organization);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(long id)
    {
        var org = await _context.Organizations.FindAsync(id);
        if (org != null)
        {
            // 检查是否有子组织
            var hasChildren = await _context.Organizations.AnyAsync(o => o.ParentId == id && !o.IsDeleted);
            if (hasChildren)
            {
                throw new InvalidOperationException("请先删除子组织");
            }

            // 检查是否有用户
            var hasUsers = await _context.Users.AnyAsync(u => u.OrganizationId == id && !u.IsDeleted);
            if (hasUsers)
            {
                throw new InvalidOperationException("该组织下存在用户，无法删除");
            }

            org.IsDeleted = true;
            org.UpdatedTime = DateTime.Now;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsCodeAsync(string code, long? excludeId = null)
    {
        return await _context.Organizations
            .AnyAsync(o => o.Code == code && !o.IsDeleted && (excludeId == null || o.Id != excludeId));
    }

    private List<Organization> BuildOrgTree(List<Organization> allOrgs, long? parentId)
    {
        return allOrgs
            .Where(o => o.ParentId == parentId)
            .Select(o =>
            {
                o.Children = BuildOrgTree(allOrgs, o.Id);
                return o;
            })
            .ToList();
    }
}
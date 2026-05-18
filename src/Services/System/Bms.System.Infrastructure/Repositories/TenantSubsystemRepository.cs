using Microsoft.EntityFrameworkCore;
using Bms.System.Domain.Entities;
using Bms.System.Domain.IRepositories;

namespace Bms.System.Infrastructure.Repositories;

/// <summary>
/// 租户子系统关联仓储实现
/// </summary>
public class TenantSubsystemRepository : ITenantSubsystemRepository
{
    private readonly SystemDbContext _context;

    public TenantSubsystemRepository(SystemDbContext context)
    {
        _context = context;
    }

    public async Task<List<TenantSubsystem>> GetByTenantIdAsync(long tenantId)
    {
        return await _context.TenantSubsystems
            .Include(ts => ts.Subsystem)
            .Where(ts => ts.TenantId == tenantId)
            .ToListAsync();
    }

    public async Task<List<TenantSubsystem>> GetBySubsystemIdAsync(long subsystemId)
    {
        return await _context.TenantSubsystems
            .Include(ts => ts.Tenant)
            .Where(ts => ts.SubsystemId == subsystemId)
            .ToListAsync();
    }

    public async Task AddRangeAsync(IEnumerable<TenantSubsystem> tenantSubsystems)
    {
        await _context.TenantSubsystems.AddRangeAsync(tenantSubsystems);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// 检查是否存在的租户子系统关联
    /// </summary>
    public async Task<bool> ExistsAsync(long tenantId, long subsystemId)
    {
        return await _context.TenantSubsystems
            .AnyAsync(ts => ts.TenantId == tenantId && ts.SubsystemId == subsystemId);
    }

    /// <summary>
    /// 恢复租户子系统关联（已弃用，不再使用软删除）
    /// </summary>
    [Obsolete("不再使用软删除，该方法已弃用")]
    public async Task RestoreAsync(long tenantId, long subsystemId)
    {
        // 无需恢复，直接返回
    }

    public async Task DeleteByTenantIdAsync(long tenantId)
    {
        var items = await _context.TenantSubsystems
            .Where(ts => ts.TenantId == tenantId)
            .ToListAsync();
        _context.TenantSubsystems.RemoveRange(items);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteByTenantIdAndSubsystemIdAsync(long tenantId, long subsystemId)
    {
        var item = await _context.TenantSubsystems
            .FirstOrDefaultAsync(ts => ts.TenantId == tenantId && ts.SubsystemId == subsystemId);
        if (item != null)
        {
            _context.TenantSubsystems.Remove(item);
            await _context.SaveChangesAsync();
        }
    }

    public async Task DeleteBySubsystemIdAsync(long subsystemId)
    {
        var items = await _context.TenantSubsystems
            .Where(ts => ts.SubsystemId == subsystemId)
            .ToListAsync();
        _context.TenantSubsystems.RemoveRange(items);
        await _context.SaveChangesAsync();
    }
}
using Microsoft.EntityFrameworkCore;
using Bms.System.Domain.Entities;
using Bms.System.Domain.IRepositories;

namespace Bms.System.Infrastructure.Repositories;

/// <summary>
/// 租户仓储实现
/// </summary>
public class TenantRepository : ITenantRepository
{
    private readonly SystemDbContext _context;

    public TenantRepository(SystemDbContext context)
    {
        _context = context;
    }

    public async Task<List<Tenant>> GetAllTenantsAsync()
    {
        return await _context.Tenants
            .Where(t => !t.IsDeleted)
            .ToListAsync();
    }
}

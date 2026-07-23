using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Bms.Store.Infrastructure.DesignTime;

/// <summary>
/// 设计时 DbContext 工厂，用于 EF Core 迁移
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<StoreDbContext>
{
    public StoreDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<StoreDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=bms_store;Username=postgres;Password=mes@2026");
        return new StoreDbContext(optionsBuilder.Options);
    }
}

using Bms.BuildingBlocks.MultiTenant.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Bms.BuildingBlocks.MultiTenant.Data;

/// <summary>
/// 多租户DbContext基类
/// </summary>
public abstract class TenantDbContext : DbContext
{
    protected TenantDbContext(
        DbContextOptions options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 为所有实现ITenant接口的实体添加查询过滤器
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ITenant).IsAssignableFrom(entityType.ClrType))
            {
                // 先不添加查询过滤器，运行时由TenantQueryFilterInterceptor动态注入
                modelBuilder.Entity(entityType.ClrType)
                    .HasAnnotation("IsTenantEntity", true);
            }
        }
    }

    public override int SaveChanges()
    {
        return base.SaveChanges();
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return base.SaveChangesAsync(cancellationToken);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }
}

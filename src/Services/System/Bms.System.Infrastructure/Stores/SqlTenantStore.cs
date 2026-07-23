using Bms.BuildingBlocks.MultiTenant.Abstractions;
using Bms.BuildingBlocks.MultiTenant.Models;
using Bms.System.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Bms.System.Infrastructure.Stores;

/// <summary>
/// SQL租户存储实现
/// </summary>
public class SqlTenantStore : ITenantStore
{
    private readonly IServiceProvider _serviceProvider;

    public SqlTenantStore(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// 根据租户ID获取租户信息
    /// </summary>
    public async Task<TenantInfo?> GetTenantByIdAsync(long tenantId, CancellationToken cancellationToken = default)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<SystemDbContext>();
        var tenant = await context.Tenants
            .AsNoTracking()
            .Where(t => !t.IsDeleted)
            .FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);

        return tenant == null ? null : MapToTenantInfo(tenant);
    }

    /// <summary>
    /// 根据租户编码获取租户信息
    /// </summary>
    public async Task<TenantInfo?> GetTenantByCodeAsync(string tenantCode, CancellationToken cancellationToken = default)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<SystemDbContext>();
        var tenant = await context.Tenants
            .AsNoTracking()
            .Where(t => !t.IsDeleted)
            .FirstOrDefaultAsync(t => t.Code == tenantCode, cancellationToken);

        return tenant == null ? null : MapToTenantInfo(tenant);
    }

    /// <summary>
    /// 获取所有租户信息（过滤已删除）
    /// </summary>
    public async Task<IEnumerable<TenantInfo>> GetAllTenantsAsync(CancellationToken cancellationToken = default)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<SystemDbContext>();
        var tenants = await context.Tenants
            .AsNoTracking()
            .Where(t => !t.IsDeleted)
            .ToListAsync(cancellationToken);

        return tenants.Select(MapToTenantInfo);
    }

    /// <summary>
    /// 更新租户信息
    /// </summary>
    public async Task<bool> UpdateTenantAsync(TenantInfo tenantInfo, CancellationToken cancellationToken = default)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<SystemDbContext>();
        var tenant = await context.Tenants.FindAsync(new object[] { tenantInfo.Id }, cancellationToken);
        if (tenant == null)
        {
            return false;
        }

        // 更新租户属性
        tenant.Name = tenantInfo.Name;
        tenant.ContactName = tenantInfo.ContactName;
        tenant.ContactPhone = tenantInfo.ContactPhone;
        tenant.ContactEmail = tenantInfo.ContactEmail;
        tenant.Status = tenantInfo.Status;
        tenant.IsolationLevel = tenantInfo.IsolationLevel;
        tenant.ConnectionString = tenantInfo.ConnectionString;
        tenant.SchemaName = tenantInfo.SchemaName;
        tenant.ExpireTime = tenantInfo.ExpireTime;
        tenant.AllowedSubsystems = tenantInfo.AllowedSubsystems;
        tenant.Remark = tenantInfo.Remark;

        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <summary>
    /// 添加租户
    /// </summary>
    public async Task<long> AddTenantAsync(TenantInfo tenantInfo, CancellationToken cancellationToken = default)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<SystemDbContext>();
        var tenant = new Tenant
        {
            Code = tenantInfo.Code,
            Name = tenantInfo.Name,
            ContactName = tenantInfo.ContactName,
            ContactPhone = tenantInfo.ContactPhone,
            ContactEmail = tenantInfo.ContactEmail,
            Status = tenantInfo.Status,
            IsolationLevel = tenantInfo.IsolationLevel,
            ConnectionString = tenantInfo.ConnectionString,
            SchemaName = tenantInfo.SchemaName,
            ExpireTime = tenantInfo.ExpireTime,
            AllowedSubsystems = tenantInfo.AllowedSubsystems,
            Remark = tenantInfo.Remark
        };

        context.Tenants.Add(tenant);
        await context.SaveChangesAsync(cancellationToken);
        return tenant.Id;
    }

    /// <summary>
    /// 删除租户（软删除）
    /// </summary>
    public async Task<bool> DeleteTenantAsync(long tenantId, CancellationToken cancellationToken = default)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<SystemDbContext>();
        var tenant = await context.Tenants.FindAsync(new object[] { tenantId }, cancellationToken);
        if (tenant == null)
        {
            return false;
        }

        // 软删除
        tenant.IsDeleted = true;
        tenant.UpdatedTime = DateTime.Now;
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <summary>
    /// 批量删除租户（软删除）
    /// </summary>
    public async Task<int> BatchDeleteAsync(IEnumerable<long> tenantIds, CancellationToken cancellationToken = default)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<SystemDbContext>();
        var ids = tenantIds.ToList();
        var tenants = await context.Tenants.Where(t => ids.Contains(t.Id)).ToListAsync(cancellationToken);
        if (!tenants.Any())
        {
            return 0;
        }

        // 软删除
        foreach (var tenant in tenants)
        {
            tenant.IsDeleted = true;
            tenant.UpdatedTime = DateTime.Now;
        }
        await context.SaveChangesAsync(cancellationToken);
        return tenants.Count;
    }

    private static TenantInfo MapToTenantInfo(Tenant tenant)
    {
        return new TenantInfo
        {
            Id = tenant.Id,
            Code = tenant.Code,
            Name = tenant.Name,
            ContactName = tenant.ContactName,
            ContactPhone = tenant.ContactPhone,
            ContactEmail = tenant.ContactEmail,
            Status = tenant.Status,
            IsolationLevel = tenant.IsolationLevel,
            ConnectionString = tenant.ConnectionString,
            SchemaName = tenant.SchemaName,
            IsEnabled = tenant.Status == 1,
            ExpireTime = tenant.ExpireTime,
            AllowedSubsystems = tenant.AllowedSubsystems,
            Remark = tenant.Remark
        };
    }
}
using Bms.BuildingBlocks.MultiTenant.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Bms.BuildingBlocks.MultiTenant.Extensions;

/// <summary>
/// 多租户DbContext扩展方法
/// </summary>
public static class DbContextExtensions
{
    /// <summary>
    /// 添加多租户DbContext
    /// </summary>
    /// <typeparam name="TDbContext">DbContext类型</typeparam>
    /// <param name="services">服务集合</param>
    /// <param name="connectionString">基础连接字符串</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddTenantDbContext<TDbContext>(
        this IServiceCollection services,
        string connectionString)
        where TDbContext : TenantDbContext
    {
        services.AddDbContext<TDbContext>((sp, options) =>
        {
            var tenantProvider = sp.GetRequiredService<Abstractions.ITenantProvider>();

            // 添加租户查询过滤器拦截器
            options.AddInterceptors(new TenantQueryFilterInterceptor(tenantProvider));
            options.AddInterceptors(new TenantSchemaInterceptor(tenantProvider));

            // 配置PostgreSQL
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorCodesToAdd: null);
            });
        });

        return services;
    }
}

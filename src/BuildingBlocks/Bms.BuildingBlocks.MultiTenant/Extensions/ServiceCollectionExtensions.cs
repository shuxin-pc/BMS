using Bms.BuildingBlocks.MultiTenant.Abstractions;
using Bms.BuildingBlocks.MultiTenant.Middleware;
using Bms.BuildingBlocks.MultiTenant.Resolvers;
using Bms.BuildingBlocks.MultiTenant.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Bms.BuildingBlocks.MultiTenant.Extensions;

/// <summary>
/// 多租户服务扩展方法
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 添加多租户支持
    /// </summary>
    /// <typeparam name="TTenantStore">租户存储实现类型</typeparam>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddMultiTenant<TTenantStore>(this IServiceCollection services)
        where TTenantStore : class, ITenantStore
    {
        services.AddHttpContextAccessor();

        // 注册租户存储
        services.AddScoped<ITenantStore, TTenantStore>();

        // 注册租户提供者
        services.AddScoped<ITenantProvider, DefaultTenantProvider>();

        // 注册租户解析策略，优先级从高到低
        services.AddScoped<ITenantResolutionStrategy, ClaimTenantResolver>();
        services.AddScoped<ITenantResolutionStrategy, HeaderTenantResolver>();
        services.AddScoped<ITenantResolutionStrategy, HostTenantResolver>();

        return services;
    }

    /// <summary>
    /// 使用多租户中间件
    /// </summary>
    /// <param name="app">应用程序构建器</param>
    /// <returns>应用程序构建器</returns>
    public static IApplicationBuilder UseMultiTenant(this IApplicationBuilder app)
    {
        app.UseMiddleware<MultiTenantMiddleware>();
        return app;
    }
}

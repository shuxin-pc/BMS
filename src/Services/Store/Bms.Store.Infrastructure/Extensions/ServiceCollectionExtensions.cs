using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Bms.BuildingBlocks.Core.Context;
using Bms.Store.Domain.Entities;
using Bms.Store.Infrastructure.Interceptors;
using Bms.Store.Infrastructure.Stores;
// 下沉后的审计日志拦截器（BuildingBlocks 版）
using AuditLogInterceptor = Bms.BuildingBlocks.Core.Interceptors.AuditLogInterceptor;

namespace Bms.Store.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddStoreServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Npgsql 8 对 List<T>/POCO + jsonb 列需显式开启动态 JSON 序列化
        // 用于 StoreReminderSetting.RoleIds（List<long> 存为 jsonb 数组）
        // GlobalTypeMapper 为进程级配置，幂等可多次调用
        NpgsqlConnection.GlobalTypeMapper.EnableDynamicJson();

        // Register interceptors
        services.AddSingleton<IdGenerationInterceptor>();
        services.AddSingleton<SoftDeleteInterceptor>();

        // Register AuditLogContext as singleton
        services.AddSingleton<IAuditLogContext, AuditLogContext>();

        // Register AuditLogInterceptor as singleton (按 Store 实体基类过滤审计范围)
        services.AddSingleton<AuditLogInterceptor>(sp =>
        {
            var auditLogContext = sp.GetRequiredService<IAuditLogContext>();
            return new AuditLogInterceptor(
                auditLogContext,
                entityType => typeof(StoreBaseEntity).IsAssignableFrom(entityType) ||
                              typeof(StoreEntityBase).IsAssignableFrom(entityType));
        });

        // Register audit log writer (审计日志直写 bms_system 库)
        services.AddScoped<IAuditLogWriter, StoreAuditLogWriter>();
        services.AddDbContext<AuditLogDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("SystemDb");
            options.UseNpgsql(connectionString);
        });

        // Register DbContext with interceptors
        services.AddDbContext<StoreDbContext>((sp, options) =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            options.UseNpgsql(connectionString);

            var idInterceptor = sp.GetRequiredService<IdGenerationInterceptor>();
            var softDeleteInterceptor = sp.GetRequiredService<SoftDeleteInterceptor>();
            var auditLogInterceptor = sp.GetRequiredService<AuditLogInterceptor>();

            options.AddInterceptors(idInterceptor, softDeleteInterceptor, auditLogInterceptor);
        });

        return services;
    }
}

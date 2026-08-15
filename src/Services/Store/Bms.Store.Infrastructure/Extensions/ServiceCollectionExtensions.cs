using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Bms.BuildingBlocks.Core.Context;
using Bms.Store.Infrastructure.Interceptors;

namespace Bms.Store.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddStoreServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Npgsql 8 对 List<T>/POCO + jsonb 列需显式开启动态 JSON 序列化
        // 用于 StoreTenantSetting.BirthdayReminderRoleIds（List<long> 存为 jsonb 数组）
        // GlobalTypeMapper 为进程级配置，幂等可多次调用
        NpgsqlConnection.GlobalTypeMapper.EnableDynamicJson();

        // Register interceptors
        services.AddSingleton<IdGenerationInterceptor>();
        services.AddSingleton<SoftDeleteInterceptor>();

        // Register AuditLogContext as singleton
        services.AddSingleton<IAuditLogContext, AuditLogContext>();

        // Register DbContext with interceptors
        services.AddDbContext<StoreDbContext>((sp, options) =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            options.UseNpgsql(connectionString);

            var idInterceptor = sp.GetRequiredService<IdGenerationInterceptor>();
            var softDeleteInterceptor = sp.GetRequiredService<SoftDeleteInterceptor>();

            options.AddInterceptors(idInterceptor, softDeleteInterceptor);
        });

        return services;
    }
}

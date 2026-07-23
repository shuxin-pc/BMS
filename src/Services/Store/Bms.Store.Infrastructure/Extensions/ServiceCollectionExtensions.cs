using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Bms.BuildingBlocks.Core.Context;
using Bms.Store.Infrastructure.Interceptors;

namespace Bms.Store.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddStoreServices(this IServiceCollection services, IConfiguration configuration)
    {
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

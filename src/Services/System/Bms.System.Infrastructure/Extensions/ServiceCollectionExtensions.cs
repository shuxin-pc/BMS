using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Bms.BuildingBlocks.Core.Context;
using Bms.BuildingBlocks.Core.Extensions;
using Bms.System.Infrastructure.Interceptors;
using Bms.System.Infrastructure.Repositories;
using Bms.System.Infrastructure.Security;
using Bms.System.Infrastructure.Services;
using Bms.System.Infrastructure.Filters;
using Bms.System.Domain.Entities;
using Bms.System.Domain.IRepositories;
using Bms.System.Domain.Interfaces;
// 下沉后的审计日志拦截器（BuildingBlocks 版），别名引用以区分可能的历史同名类
using AuditLogInterceptor = Bms.BuildingBlocks.Core.Interceptors.AuditLogInterceptor;

namespace Bms.System.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSystemServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Register interceptors
        services.AddSingleton<IdGenerationInterceptor>();
        services.AddSingleton<SoftDeleteInterceptor>();

        // Register AuditLogContext as singleton (AsyncLocal provides thread-safety)
        services.AddSingleton<IAuditLogContext, AuditLogContext>();

        // Register AuditLogInterceptor as singleton (下沉版，按 System 实体基类过滤以保持既有审计范围)
        services.AddSingleton<AuditLogInterceptor>(sp =>
        {
            var auditLogContext = sp.GetRequiredService<IAuditLogContext>();
            return new AuditLogInterceptor(
                auditLogContext,
                entityType => typeof(BaseEntity).IsAssignableFrom(entityType));
        });

        // Register audit log writer (写入 System 库 AuditLog 表)
        services.AddScoped<IAuditLogWriter, SystemAuditLogWriter>();

        // Register security services
        services.AddScoped<IPasswordHasher, PasswordHasher>();

        // Register data permission filter
        services.AddScoped<IDataPermissionFilter, DataPermissionFilter>();

        // Register user permission checker
        services.AddScoped<IUserPermissionChecker, UserPermissionChecker>();

        // Register DbContext with interceptors
        services.AddDbContext<SystemDbContext>((sp, options) =>
        {
            var connectionString = configuration.GetConnectionString("SystemDb");
            options.UseNpgsql(connectionString);

            var idInterceptor = sp.GetRequiredService<IdGenerationInterceptor>();
            var softDeleteInterceptor = sp.GetRequiredService<SoftDeleteInterceptor>();
            var auditLogInterceptor = sp.GetRequiredService<AuditLogInterceptor>();

            options.AddInterceptors(idInterceptor, softDeleteInterceptor, auditLogInterceptor);
        });


        // Register repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IMenuRepository, MenuRepository>();
        services.AddScoped<IOrganizationRepository, OrganizationRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<ISystemConfigRepository, SystemConfigRepository>();
        services.AddScoped<IDataPermissionRepository, DataPermissionRepository>();
        services.AddScoped<ISubsystemRepository, SubsystemRepository>();
        services.AddScoped<ISubsystemMenuRepository, SubsystemMenuRepository>();
        services.AddScoped<ITenantSubsystemRepository, TenantSubsystemRepository>();
        services.AddScoped<IRoleMenuAuthRepository, RoleMenuAuthRepository>();
        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<IMessageRepository, MessageRepository>();
        services.AddScoped<IMessageRecipientRepository, MessageRecipientRepository>();

        return services;
    }
}
using Microsoft.Extensions.DependencyInjection;
using Bms.System.Application.Services;
using Bms.System.Application.Validators;

namespace Bms.System.Application.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // 注册应用服务
        services.AddScoped<IUserAppService, UserAppService>();
        services.AddScoped<IRoleAppService, RoleAppService>();
        services.AddScoped<IMenuAppService, MenuAppService>();
        services.AddScoped<IOrganizationAppService, OrganizationAppService>();
        services.AddScoped<IAuditLogAppService, AuditLogAppService>();
        services.AddScoped<ISystemConfigAppService, SystemConfigAppService>();
        services.AddScoped<ISystemConfigService, SystemConfigService>();
        services.AddScoped<IDataPermissionAppService, DataPermissionAppService>();
        services.AddScoped<IProfileAppService, ProfileAppService>();
        services.AddScoped<ITenantAppService, TenantAppService>();
        services.AddScoped<IAuthAppService, AuthAppService>();
        services.AddScoped<ISubsystemAppService, SubsystemAppService>();
        services.AddScoped<ITenantSubsystemAppService, TenantSubsystemAppService>();
        services.AddScoped<IRoleMenuAuthAppService, RoleMenuAuthAppService>();
        services.AddScoped<ISubsystemMigrationService, SubsystemMigrationService>();
        services.AddScoped<IMessageAppService, MessageAppService>();

        // 全局搜索（用户分组）
        services.AddScoped<IGlobalSearchAppService, GlobalSearchAppService>();

        // 注册 FluentValidation 验证器
        services.AddFluentValidationServices();

        return services;
    }
}
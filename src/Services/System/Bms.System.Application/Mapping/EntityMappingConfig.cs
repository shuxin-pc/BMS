using Mapster;
using Bms.System.Application.Dtos.Users;
using Bms.System.Application.Dtos.Roles;
using Bms.System.Application.Dtos.Menus;
using Bms.System.Application.Dtos.Organizations;
using Bms.System.Application.Dtos.DataPermissions;
using Bms.System.Application.Dtos.Tenants;
using Bms.System.Application.Dtos.AuditLogs;
using Bms.System.Application.Dtos.SystemConfigs;
using Bms.System.Application.Dtos.Subsystems;
using Bms.System.Application.Dtos.Messages;
using Bms.System.Domain.Entities;
using Bms.BuildingBlocks.MultiTenant.Models;

namespace Bms.System.Application.Mapping;

/// <summary>
/// 实体映射配置
/// </summary>
public static class EntityMappingConfig
{
    /// <summary>
    /// 配置实体映射关系
    /// </summary>
    public static void Configure()
    {
        // User 实体映射
        TypeAdapterConfig<User, UserDto>
            .NewConfig()
            .Map(dest => dest.OrganizationName, src => src.Organization != null ? src.Organization.Name : null);

        // Role 实体映射
        TypeAdapterConfig<Role, RoleDto>
            .NewConfig()
            .Map(dest => dest.DataScopeType, src => src.DataPermission != null ? src.DataPermission.DataScopeType : 0)
            .Map(dest => dest.CustomOrganizationIds, src => src.DataPermission != null ? src.DataPermission.CustomOrganizationIds : null);

        // Menu 实体映射
        TypeAdapterConfig<Menu, MenuDto>
            .NewConfig();

        // Organization 实体映射
        TypeAdapterConfig<Organization, OrganizationDto>
            .NewConfig()
            .Map(dest => dest.Children, src => src.Children.Select(c => c.Adapt<OrganizationDto>()).ToList());

        // DataPermission 实体映射
        TypeAdapterConfig<DataPermission, DataPermissionDto>
            .NewConfig();

        // AuditLog 实体映射
        TypeAdapterConfig<AuditLog, AuditLogDto>
            .NewConfig();

        // SystemConfig 实体映射
        TypeAdapterConfig<SystemConfig, SystemConfigDto>
            .NewConfig()
            .Map(dest => dest.TenantCode, src => src.TenantCode);

        // Subsystem 实体映射
        TypeAdapterConfig<Subsystem, SubsystemDto>
            .NewConfig();

        // Message 实体映射（管理端）
        TypeAdapterConfig<Message, MessageDto>
            .NewConfig();

        // MessageRecipient + Message -> MessageInboxDto（收件箱项）
        TypeAdapterConfig<MessageRecipient, MessageInboxDto>
            .NewConfig()
            .Map(dest => dest.Id, src => src.Id);

        // TenantInfo 映射到 TenantDto（租户管理使用 TenantInfo）
        TypeAdapterConfig<TenantInfo, TenantDto>
            .NewConfig()
            .Map(dest => dest.IsEnabled, src => src.Status == 1)
            .Map(dest => dest.AllowedSubsystemList, src => src.GetAllowedSubsystemList())
            .Map(dest => dest.CreatedAt, src => DateTime.Now);

        // Tenant 实体映射到 TenantDto
        TypeAdapterConfig<Tenant, TenantDto>
            .NewConfig()
            .Map(dest => dest.IsEnabled, src => src.Status == 1)
            .Map(dest => dest.AllowedSubsystemList, src => string.IsNullOrEmpty(src.AllowedSubsystems)
                ? null
                : src.AllowedSubsystems.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList())
            .Map(dest => dest.CreatedAt, src => src.CreatedTime);
    }
}

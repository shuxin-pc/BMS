using Bms.BuildingBlocks.Core.IdGenerator;
using Bms.BuildingBlocks.MultiTenant.Models;
using Bms.System.Domain.Entities;
using Bms.System.Domain.Enums;
using Bms.System.Domain.Interfaces;

namespace Bms.System.Infrastructure.SeedData;

/// <summary>
/// 系统种子数据
/// </summary>
public static class SystemSeedData
{
    /// <summary>
    /// 初始化种子数据
    /// </summary>
    public static void Initialize(SystemDbContext context, ISnowflakeIdGenerator idGenerator, IPasswordHasher passwordHasher)
    {
        // 检查是否已初始化
        if (context.Users.Any())
        {
            return;
        }

        // 创建默认租户（如果没有租户数据）
        CreateDefaultTenants(context, idGenerator);

        // 创建角色
        var superAdminRoleId = idGenerator.NewId();
        var tenantAdminRoleId = idGenerator.NewId();

        // 创建超级管理员角色（属于平台租户，TenantId=1）
        var superAdminRole = new Role
        {
            Id = superAdminRoleId,
            Name = "超级管理员",
            Code = "super_admin",
            Description = "系统超级管理员，拥有所有权限，可管理所有租户",
            IsSystem = true,
            Status = 1,
            Level = 0,
            CreatedTime = DateTime.Now,
            TenantId = 1,
            TenantCode = "platform"
        };
        context.Roles.Add(superAdminRole);

        // 创建管理员角色（属于平台租户，TenantId=1）
        var tenantAdminRole = new Role
        {
            Id = tenantAdminRoleId,
            Name = "管理员",
            Code = "tenant_admin",
            Description = "管理员，管理本租户内的用户和数据",
            IsSystem = true,
            Status = 1,
            Level = 1,
            CreatedTime = DateTime.Now,
            TenantId = 1,
            TenantCode = "platform"
        };
        context.Roles.Add(tenantAdminRole);

        // 为管理员角色创建默认数据权限（全部数据）
        var tenantAdminDataPermission = new DataPermission
        {
            RoleId = tenantAdminRoleId,
            DataScopeType = (int)DataScopeType.All, // 全部数据
            CustomOrganizationIds = null,
            CreatedTime = DateTime.Now
        };
        context.DataPermissions.Add(tenantAdminDataPermission);

        // 创建超级管理员用户（属于平台租户）
        var adminUserId = idGenerator.NewId();
        var adminUser = new User
        {
            Id = adminUserId,
            UserName = "admin",
            RealName = "系统管理员",
            Email = "admin@mes.com",
            Phone = "13800000000",
            PasswordHash = passwordHasher.HashPassword("Admin@123"),
            Status = (int)UserStatus.Normal,
            CreatedTime = DateTime.Now,
            // 超级管理员属于平台租户（ID=1）
            TenantId = 1,
            TenantCode = "platform",
            // 种子用户由平台租户创建
            CreatorTenantId = 1
        };
        context.Users.Add(adminUser);

        // 关联超级管理员和角色
        var adminUserRoleId = idGenerator.NewId();
        var adminUserRole = new UserRole
        {
            Id = adminUserRoleId,
            UserId = adminUserId,
            RoleId = superAdminRoleId,
            CreatedTime = DateTime.Now
        };
        context.UserRoles.Add(adminUserRole);

        // 创建默认子系统（先创建，获取ID后再创建菜单，以便将按钮分配给子系统）
        var basicDataSubsystemId = idGenerator.NewId();
        var basicDataSubsystem = new Subsystem
        {
            Id = basicDataSubsystemId,
            Code = "BasicDataManagement",
            Name = "基础数据",
            Icon = "Grid",
            Description = "基础数据管理子系统，包含系统管理等基础功能",
            Sort = 1,
            Status = 1,
            CreatedTime = DateTime.Now
        };
        context.Subsystems.Add(basicDataSubsystem);
        context.SaveChanges();

        // 创建默认菜单（返回菜单ID映射，便于后续关联）
        var menuIdMap = CreateDefaultMenus(context, idGenerator, basicDataSubsystemId);
        context.SaveChanges();

        // 将子系统分配给平台租户
        var tenantSubsystem = new TenantSubsystem
        {
            TenantId = 1, // 平台租户
            SubsystemId = basicDataSubsystemId,
            CreatedTime = DateTime.Now
        };
        context.TenantSubsystems.Add(tenantSubsystem);

        // 将菜单权限分配给超级管理员角色（授权所有菜单和按钮，与页面保存逻辑一致）
        // 页面保存时 getCheckedKeys(false) 返回所有完全勾选的节点（菜单+按钮），半勾选的目录不存储
        // 目录由前端根据子菜单状态自动推导
        foreach (var kvp in menuIdMap)
        {
            // 排除目录（"system" 对应系统管理目录，type=0），目录由前端根据子菜单状态推导
            if (kvp.Key == "system")
            {
                continue;
            }
            var roleMenuAuth = new RoleMenuAuth
            {
                RoleId = superAdminRoleId,
                MenuId = kvp.Value,
                SubsystemId = basicDataSubsystemId,
                CreatedTime = DateTime.Now
            };
            context.RoleMenuAuths.Add(roleMenuAuth);
        }

        // 保存所有变更
        context.SaveChanges();
    }

    /// <summary>
    /// 初始化系统配置（独立于用户数据，用于更新已有数据库）
    /// </summary>
    public static void InitializeSystemConfigs(SystemDbContext context)
    {
        var now = DateTime.Now;

        // System（系统设置）
        // 键值, 默认值, 分组, 描述, 公开, 可修改, 排序, 时间戳
        AddConfigIfNotExists(context, "SystemName", "BMS后台管理系统", "System", "系统名称", true, false, 1, now);
        AddConfigIfNotExists(context, "SystemEnglishName", "Backend Management System", "System", "系统英文名", true, false, 2, now);
        AddConfigIfNotExists(context, "SystemShortName", "BMS", "System", "系统简称", true, false, 3, now);
        AddConfigIfNotExists(context, "SystemVersion", "2.0", "System", "系统版本", true, false, 4, now);
        AddConfigIfNotExists(context, "LoginLogo", "<svg viewBox=\"0 0 48 48\" fill=\"none\" xmlns=\"http://www.w3.org/2000/svg\"><rect x=\"4\" y=\"10\" width=\"40\" height=\"28\" rx=\"3\" stroke=\"currentColor\" stroke-width=\"2.5\"/><path d=\"M10 10V8C10 6.34315 11.3431 5 13 5H35C36.6569 5 38 6.34315 38 8V10\" stroke=\"currentColor\" stroke-width=\"2.5\"/><circle cx=\"24\" cy=\"24\" r=\"7\" stroke=\"currentColor\" stroke-width=\"2.5\"/><path d=\"M17 31H31\" stroke=\"currentColor\" stroke-width=\"2.5\" stroke-linecap=\"round\"/><circle cx=\"24\" cy=\"24\" r=\"2\" fill=\"currentColor\"/></svg>", "System", "登录页Logo", true, false, 5, now);
        AddConfigIfNotExists(context, "SystemLogo", "<svg viewBox=\"0 0 40 40\" fill=\"none\" xmlns=\"http://www.w3.org/2000/svg\"><rect x=\"2\" y=\"8\" width=\"36\" height=\"24\" rx=\"2\" stroke=\"currentColor\" stroke-width=\"2\"/><path d=\"M8 8V6C8 4.89543 8.89543 4 10 4H30C31.1046 4 32 4.89543 32 6V8\" stroke=\"currentColor\" stroke-width=\"2\"/><circle cx=\"20\" cy=\"20\" r=\"6\" stroke=\"currentColor\" stroke-width=\"2\"/><path d=\"M14 26H26\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\"/></svg>", "System", "框架左上角Logo", true, false, 6, now);
        AddConfigIfNotExists(context, "SystemCopyright", "©2026 BMS保留所有权利", "System", "系统版权信息", true, false, 7, now);
        AddConfigIfNotExists(context, "DefaultPageSize", "20", "System", "默认分页大小", true, true, 8, now);
        AddConfigIfNotExists(context, "DefaultPageSizes", "10,15,20,50", "System", "分页选项列表", true, false, 9, now);

        // Security（安全策略）
        AddConfigIfNotExists(context, "PasswordMinLength", "8", "Security", "密码最小长度", true, false, 1, now);
        AddConfigIfNotExists(context, "PasswordMaxLength", "32", "Security", "密码最大长度", true, false, 2, now);
        AddConfigIfNotExists(context, "PasswordRequireUppercase", "true", "Security", "必须包含大写字母", true, false, 3, now);
        AddConfigIfNotExists(context, "PasswordRequireLowercase", "true", "Security", "必须包含小写字母", true, false, 4, now);
        AddConfigIfNotExists(context, "PasswordRequireNumber", "true", "Security", "必须包含数字", true, false, 5, now);
        AddConfigIfNotExists(context, "PasswordRequireSpecialChar", "true", "Security", "必须包含特殊字符", true, false, 6, now);

        // AuditLog（审计日志）
        AddConfigIfNotExists(context, "EnableAuditLog", "true", "AuditLog", "是否启用审计日志", true, true, 1, now);
        AddConfigIfNotExists(context, "AuditLogRetentionDays", "30", "AuditLog", "审计日志保留天数", true, true, 2, now);
        AddConfigIfNotExists(context, "EnableLoginAudit", "true", "AuditLog", "启用登录/登出审计", true, true, 3, now);
        AddConfigIfNotExists(context, "AuditLogExcludePaths", "/health,/swagger", "AuditLog", "排除路径（逗号分隔）", false, false, 4, now);

        // Upload（文件上传）
        AddConfigIfNotExists(context, "MaxAvatarSize", "2", "Upload", "头像最大大小(MB)", true, false, 1, now);
        AddConfigIfNotExists(context, "MaxUploadSize", "10", "Upload", "通用最大上传大小(MB)", true, false, 2, now);
        AddConfigIfNotExists(context, "AllowedFileExtensions", "jpg,jpeg,png,pdf,xls,xlsx,doc,docx,txt", "Upload", "允许的文件扩展名（逗号分隔）", true, false, 3, now);

        context.SaveChanges();
    }

    /// <summary>
    /// 补全所有页面菜单的"页面查看"按钮
    /// 为每个 type=1 菜单检查并创建"页面查看"按钮（code=菜单code:view），并授权给超级管理员。
    /// 此方法幂等，可安全重复调用，用于已有数据库的增量迁移。
    /// </summary>
    public static void EnsurePageViewButtons(SystemDbContext context, ISnowflakeIdGenerator idGenerator)
    {
        var now = DateTime.Now;

        // 查询超级管理员角色
        var superAdminRole = context.Roles.FirstOrDefault(r => r.Code == "super_admin" && !r.IsDeleted);
        if (superAdminRole == null)
        {
            return;
        }

        // 查询所有页面菜单（type=1）
        var pageMenus = context.Menus.Where(m => m.Type == 1 && !m.IsDeleted).ToList();
        if (pageMenus.Count == 0)
        {
            return;
        }

        // 查询所有 type=2 按钮，建立 code 集合用于判断"页面查看"按钮是否已存在
        var existingButtons = context.Menus
            .Where(m => m.Type == 2 && !m.IsDeleted)
            .ToList();
        var existingButtonCodes = existingButtons.Select(b => b.Code).ToHashSet();

        // 查询菜单所属子系统映射
        var menuSubsystemMap = context.SubsystemMenus
            .ToDictionary(sm => sm.MenuId, sm => sm.SubsystemId);

        // 查询超级管理员已授权的菜单ID集合
        var superAdminMenuIds = context.RoleMenuAuths
            .Where(rma => rma.RoleId == superAdminRole.Id)
            .Select(rma => rma.MenuId)
            .ToHashSet();

        var hasChanges = false;

        foreach (var pageMenu in pageMenus)
        {
            var buttonCode = $"{pageMenu.Code}:view";

            // 已存在相同 code 的按钮
            if (existingButtonCodes.Contains(buttonCode))
            {
                // 查找属于当前菜单的冲突按钮（如旧的"查看详情"占用了 :view code）
                var conflictButton = existingButtons.FirstOrDefault(b => b.Code == buttonCode && b.ParentId == pageMenu.Id);
                if (conflictButton != null && conflictButton.Name != "页面查看")
                {
                    // 将冲突按钮的 code 改为 :detail，释放 :view 给"页面查看"按钮
                    var detailCode = $"{pageMenu.Code}:detail";
                    conflictButton.Code = detailCode;
                    conflictButton.PermissionCode = detailCode;
                    conflictButton.UpdatedTime = now;
                    existingButtonCodes.Remove(buttonCode);
                    existingButtonCodes.Add(detailCode);
                    hasChanges = true;
                    // 继续创建"页面查看"按钮
                }
                else
                {
                    // 已是"页面查看"按钮或被其他菜单的按钮占用 code，跳过
                    continue;
                }
            }

            // 查找菜单所属子系统
            if (!menuSubsystemMap.TryGetValue(pageMenu.Id, out var subsystemId))
            {
                continue;
            }

            // 创建"页面查看"按钮
            var buttonId = idGenerator.NewId();
            context.Menus.Add(new Menu
            {
                Id = buttonId,
                ParentId = pageMenu.Id,
                Name = "页面查看",
                Code = buttonCode,
                Path = null,
                Component = null,
                Icon = null,
                Type = (int)MenuType.Button,
                Sort = 0,
                Status = 1,
                IsVisible = true,
                IsCache = false,
                IsAlwaysShow = false,
                PermissionCode = buttonCode,
                CreatedTime = now
            });

            // 关联按钮到子系统
            context.SubsystemMenus.Add(new SubsystemMenu
            {
                SubsystemId = subsystemId,
                MenuId = buttonId,
                CreatedTime = now
            });

            // 授权给超级管理员
            context.RoleMenuAuths.Add(new RoleMenuAuth
            {
                RoleId = superAdminRole.Id,
                MenuId = buttonId,
                SubsystemId = subsystemId,
                CreatedTime = now
            });
            superAdminMenuIds.Add(buttonId);

            existingButtonCodes.Add(buttonCode);
            hasChanges = true;
        }

        if (hasChanges)
        {
            context.SaveChanges();
        }
    }

    /// <summary>
    /// 添加配置项（如果不存在）
    /// </summary>
    private static void AddConfigIfNotExists(
        SystemDbContext context,
        string configKey,
        string configValue,
        string configGroup,
        string description,
        bool isPublic,
        bool isEditable,
        int sort,
        DateTime now)
    {
        if (!context.SystemConfigs.Any(c => c.ConfigKey == configKey))
        {
            var config = new SystemConfig
            {
                ConfigKey = configKey,
                ConfigValue = configValue,
                ConfigGroup = configGroup,
                Description = description,
                IsPublic = isPublic,
                IsEditable = isEditable,
                // 种子数据创建的配置属于系统配置，禁止删除
                IsSystem = true,
                Sort = sort,
                TenantId = 1,
                TenantCode = "platform",
                CreatedTime = now
            };
            context.SystemConfigs.Add(config);
        }
    }

    /// <summary>
    /// 创建默认菜单并返回菜单ID映射（便于后续关联）
    /// </summary>
    private static Dictionary<string, long> CreateDefaultMenus(SystemDbContext context, ISnowflakeIdGenerator idGenerator, long subsystemId)
    {
        var menuIdMap = new Dictionary<string, long>();
        var now = DateTime.Now;

        // 首页菜单
        var homeMenuId = idGenerator.NewId();
        menuIdMap["system:home"] = homeMenuId;
        context.Menus.Add(new Menu
        {
            Id = homeMenuId,
            ParentId = null,
            Name = "首页",
            Code = "system:home",
            Path = "/dashboard",
            Component = "dashboard/index",
            Icon = "HomeFilled",
            Type = (int)MenuType.Menu,
            Sort = 0,
            Status = 1,
            IsVisible = true,
            IsCache = false,
            IsAlwaysShow = true,
            CreatedTime = now
        });
        // 将首页菜单分配给子系统
        context.SubsystemMenus.Add(new SubsystemMenu
        {
            SubsystemId = subsystemId,
            MenuId = homeMenuId,
            CreatedTime = now
        });

        // 系统管理一级菜单
        var systemMenuId = idGenerator.NewId();
        menuIdMap["system"] = systemMenuId;
        context.Menus.Add(new Menu
        {
            Id = systemMenuId,
            ParentId = null,
            Name = "系统管理",
            Code = "system",
            Path = "/system",
            Component = null,
            Icon = "Setting",
            Type = (int)MenuType.Directory,
            Sort = 1,
            Status = 1,
            IsVisible = true,
            IsCache = false,
            IsAlwaysShow = false,
            CreatedTime = now
        });
        // 将系统管理目录分配给子系统
        context.SubsystemMenus.Add(new SubsystemMenu
        {
            SubsystemId = subsystemId,
            MenuId = systemMenuId,
            CreatedTime = now
        });

        // 用户管理
        var userMenuId = idGenerator.NewId();
        menuIdMap["system:user"] = userMenuId;
        context.Menus.Add(new Menu
        {
            Id = userMenuId,
            ParentId = systemMenuId,
            Name = "用户管理",
            Code = "system:user",
            Path = "system/users",
            Component = "system/users/index",
            Icon = "User",
            Type = (int)MenuType.Menu,
            Sort = 1,
            Status = 1,
            IsVisible = true,
            IsCache = false,
            IsAlwaysShow = false,
            PermissionCode = "system:user:view",
            CreatedTime = now
        });
        // 将用户管理菜单分配给子系统
        context.SubsystemMenus.Add(new SubsystemMenu
        {
            SubsystemId = subsystemId,
            MenuId = userMenuId,
            CreatedTime = now
        });

        // 角色管理
        var roleMenuId = idGenerator.NewId();
        menuIdMap["system:role"] = roleMenuId;
        context.Menus.Add(new Menu
        {
            Id = roleMenuId,
            ParentId = systemMenuId,
            Name = "角色管理",
            Code = "system:role",
            Path = "system/roles",
            Component = "system/roles/index",
            Icon = "Lock",
            Type = (int)MenuType.Menu,
            Sort = 2,
            Status = 1,
            IsVisible = true,
            IsCache = false,
            IsAlwaysShow = false,
            PermissionCode = "system:role:view",
            CreatedTime = now
        });
        // 将角色管理菜单分配给子系统
        context.SubsystemMenus.Add(new SubsystemMenu
        {
            SubsystemId = subsystemId,
            MenuId = roleMenuId,
            CreatedTime = now
        });

        // 菜单管理
        var menuMenuId = idGenerator.NewId();
        menuIdMap["system:menu"] = menuMenuId;
        context.Menus.Add(new Menu
        {
            Id = menuMenuId,
            ParentId = systemMenuId,
            Name = "菜单管理",
            Code = "system:menu",
            Path = "system/menus",
            Component = "system/menus/index",
            Icon = "Menu",
            Type = (int)MenuType.Menu,
            Sort = 3,
            Status = 1,
            IsVisible = true,
            IsCache = false,
            IsAlwaysShow = false,
            PermissionCode = "system:menu:view",
            CreatedTime = now
        });
        // 将菜单管理菜单分配给子系统
        context.SubsystemMenus.Add(new SubsystemMenu
        {
            SubsystemId = subsystemId,
            MenuId = menuMenuId,
            CreatedTime = now
        });

        // 组织架构管理
        var orgMenuId = idGenerator.NewId();
        menuIdMap["system:organization"] = orgMenuId;
        context.Menus.Add(new Menu
        {
            Id = orgMenuId,
            ParentId = systemMenuId,
            Name = "组织架构",
            Code = "system:organization",
            Path = "system/organizations",
            Component = "system/organizations/index",
            Icon = "OfficeBuilding",
            Type = (int)MenuType.Menu,
            Sort = 4,
            Status = 1,
            IsVisible = true,
            IsCache = false,
            IsAlwaysShow = false,
            PermissionCode = "system:organization:view",
            CreatedTime = now
        });
        // 将组织架构菜单分配给子系统
        context.SubsystemMenus.Add(new SubsystemMenu
        {
            SubsystemId = subsystemId,
            MenuId = orgMenuId,
            CreatedTime = now
        });

        // 租户管理
        var tenantMenuId = idGenerator.NewId();
        menuIdMap["system:tenant"] = tenantMenuId;
        context.Menus.Add(new Menu
        {
            Id = tenantMenuId,
            ParentId = systemMenuId,
            Name = "租户管理",
            Code = "system:tenant",
            Path = "system/tenants",
            Component = "system/tenants/index",
            Icon = "House",
            Type = (int)MenuType.Menu,
            Sort = 5,
            Status = 1,
            IsVisible = true,
            IsCache = false,
            IsAlwaysShow = false,
            PermissionCode = "system:tenant:view",
            CreatedTime = now
        });
        // 将租户管理菜单分配给子系统
        context.SubsystemMenus.Add(new SubsystemMenu
        {
            SubsystemId = subsystemId,
            MenuId = tenantMenuId,
            CreatedTime = now
        });

        // 审计日志
        var auditLogMenuId = idGenerator.NewId();
        menuIdMap["system:auditLog"] = auditLogMenuId;
        context.Menus.Add(new Menu
        {
            Id = auditLogMenuId,
            ParentId = systemMenuId,
            Name = "审计日志",
            Code = "system:auditLog",
            Path = "system/audit-logs",
            Component = "system/audit-logs/index",
            Icon = "Document",
            Type = (int)MenuType.Menu,
            Sort = 6,
            Status = 1,
            IsVisible = true,
            IsCache = false,
            IsAlwaysShow = false,
            PermissionCode = "system:auditLog:view",
            CreatedTime = now
        });
        // 将审计日志菜单分配给子系统
        context.SubsystemMenus.Add(new SubsystemMenu
        {
            SubsystemId = subsystemId,
            MenuId = auditLogMenuId,
            CreatedTime = now
        });

        // 系统配置
        var configMenuId = idGenerator.NewId();
        menuIdMap["system:config"] = configMenuId;
        context.Menus.Add(new Menu
        {
            Id = configMenuId,
            ParentId = systemMenuId,
            Name = "系统配置",
            Code = "system:config",
            Path = "system/system-configs",
            Component = "system/system-configs/index",
            Icon = "Tools",
            Type = (int)MenuType.Menu,
            Sort = 7,
            Status = 1,
            IsVisible = true,
            IsCache = false,
            IsAlwaysShow = false,
            PermissionCode = "system:config:view",
            CreatedTime = now
        });
        // 将系统配置菜单分配给子系统
        context.SubsystemMenus.Add(new SubsystemMenu
        {
            SubsystemId = subsystemId,
            MenuId = configMenuId,
            CreatedTime = now
        });

        // 子系统管理
        var subsystemMenuId = idGenerator.NewId();
        menuIdMap["system:subsystem"] = subsystemMenuId;
        context.Menus.Add(new Menu
        {
            Id = subsystemMenuId,
            ParentId = systemMenuId,
            Name = "子系统管理",
            Code = "system:subsystem",
            Path = "system/subsystems",
            Component = "system/subsystems/index",
            Icon = "Grid",
            Type = (int)MenuType.Menu,
            Sort = 8,
            Status = 1,
            IsVisible = true,
            IsCache = false,
            IsAlwaysShow = false,
            PermissionCode = "system:subsystem:view",
            CreatedTime = now
        });
        // 将子系统管理菜单分配给子系统
        context.SubsystemMenus.Add(new SubsystemMenu
        {
            SubsystemId = subsystemId,
            MenuId = subsystemMenuId,
            CreatedTime = now
        });

        // 消息发送记录
        var messageSentMenuId = idGenerator.NewId();
        menuIdMap["system:message:sent"] = messageSentMenuId;
        context.Menus.Add(new Menu
        {
            Id = messageSentMenuId,
            ParentId = systemMenuId,
            Name = "消息发送记录",
            Code = "system:message:sent",
            Path = "system/messages/sent",
            Component = "system/messages/sent/index",
            Icon = "Promotion",
            Type = (int)MenuType.Menu,
            Sort = 10,
            Status = 1,
            IsVisible = true,
            IsCache = false,
            IsAlwaysShow = false,
            PermissionCode = "system:message:sent:view",
            CreatedTime = now
        });
        // 将消息发送记录菜单分配给子系统
        context.SubsystemMenus.Add(new SubsystemMenu
        {
            SubsystemId = subsystemId,
            MenuId = messageSentMenuId,
            CreatedTime = now
        });

        // 用户管理按钮
        CreateButtonMenu(context, idGenerator, now, userMenuId, "新增用户", "system:user:add", 1, subsystemId, menuIdMap);
        CreateButtonMenu(context, idGenerator, now, userMenuId, "编辑用户", "system:user:edit", 2, subsystemId, menuIdMap);
        CreateButtonMenu(context, idGenerator, now, userMenuId, "删除用户", "system:user:delete", 3, subsystemId, menuIdMap);
        CreateButtonMenu(context, idGenerator, now, userMenuId, "批量删除", "system:user:batchDelete", 4, subsystemId, menuIdMap);
        CreateButtonMenu(context, idGenerator, now, userMenuId, "重置密码", "system:user:resetPwd", 5, subsystemId, menuIdMap);

        // 角色管理按钮
        CreateButtonMenu(context, idGenerator, now, roleMenuId, "新增角色", "system:role:add", 1, subsystemId, menuIdMap);
        CreateButtonMenu(context, idGenerator, now, roleMenuId, "编辑角色", "system:role:edit", 2, subsystemId, menuIdMap);
        CreateButtonMenu(context, idGenerator, now, roleMenuId, "删除角色", "system:role:delete", 3, subsystemId, menuIdMap);
        CreateButtonMenu(context, idGenerator, now, roleMenuId, "批量删除", "system:role:batchDelete", 4, subsystemId, menuIdMap);
        CreateButtonMenu(context, idGenerator, now, roleMenuId, "查看详情", "system:role:detail", 5, subsystemId, menuIdMap);
        CreateButtonMenu(context, idGenerator, now, roleMenuId, "保存权限", "system:role:saveAuth", 6, subsystemId, menuIdMap);

        // 菜单管理按钮
        CreateButtonMenu(context, idGenerator, now, menuMenuId, "新增菜单", "system:menu:add", 1, subsystemId, menuIdMap);
        CreateButtonMenu(context, idGenerator, now, menuMenuId, "编辑菜单", "system:menu:edit", 2, subsystemId, menuIdMap);
        CreateButtonMenu(context, idGenerator, now, menuMenuId, "删除菜单", "system:menu:delete", 3, subsystemId, menuIdMap);

        // 组织架构按钮
        CreateButtonMenu(context, idGenerator, now, orgMenuId, "新增组织", "system:organization:add", 1, subsystemId, menuIdMap);
        CreateButtonMenu(context, idGenerator, now, orgMenuId, "新增子级", "system:organization:addChild", 2, subsystemId, menuIdMap);
        CreateButtonMenu(context, idGenerator, now, orgMenuId, "编辑组织", "system:organization:edit", 3, subsystemId, menuIdMap);
        CreateButtonMenu(context, idGenerator, now, orgMenuId, "删除组织", "system:organization:delete", 4, subsystemId, menuIdMap);

        // 租户管理按钮
        CreateButtonMenu(context, idGenerator, now, tenantMenuId, "新增租户", "system:tenant:add", 1, subsystemId, menuIdMap);
        CreateButtonMenu(context, idGenerator, now, tenantMenuId, "编辑租户", "system:tenant:edit", 2, subsystemId, menuIdMap);
        CreateButtonMenu(context, idGenerator, now, tenantMenuId, "删除租户", "system:tenant:delete", 3, subsystemId, menuIdMap);
        CreateButtonMenu(context, idGenerator, now, tenantMenuId, "批量删除", "system:tenant:batchDelete", 4, subsystemId, menuIdMap);
        CreateButtonMenu(context, idGenerator, now, tenantMenuId, "查看详情", "system:tenant:detail", 5, subsystemId, menuIdMap);
        CreateButtonMenu(context, idGenerator, now, tenantMenuId, "保存子系统", "system:tenant:saveSubsystems", 6, subsystemId, menuIdMap);

        // 审计日志按钮
        CreateButtonMenu(context, idGenerator, now, auditLogMenuId, "查看详情", "system:auditLog:detail", 1, subsystemId, menuIdMap);
        CreateButtonMenu(context, idGenerator, now, auditLogMenuId, "导出日志", "system:auditLog:export", 2, subsystemId, menuIdMap);
        CreateButtonMenu(context, idGenerator, now, auditLogMenuId, "清空历史", "system:auditLog:clearHistory", 3, subsystemId, menuIdMap);

        // 系统配置按钮
        CreateButtonMenu(context, idGenerator, now, configMenuId, "新增配置", "system:config:add", 1, subsystemId, menuIdMap);
        CreateButtonMenu(context, idGenerator, now, configMenuId, "编辑配置", "system:config:edit", 2, subsystemId, menuIdMap);
        CreateButtonMenu(context, idGenerator, now, configMenuId, "删除配置", "system:config:delete", 3, subsystemId, menuIdMap);

        // 子系统管理按钮
        CreateButtonMenu(context, idGenerator, now, subsystemMenuId, "新增子系统", "system:subsystem:add", 1, subsystemId, menuIdMap);
        CreateButtonMenu(context, idGenerator, now, subsystemMenuId, "编辑子系统", "system:subsystem:edit", 2, subsystemId, menuIdMap);
        CreateButtonMenu(context, idGenerator, now, subsystemMenuId, "删除子系统", "system:subsystem:delete", 3, subsystemId, menuIdMap);
        CreateButtonMenu(context, idGenerator, now, subsystemMenuId, "分配菜单", "system:subsystem:assignMenu", 4, subsystemId, menuIdMap);

        // 消息发送记录按钮
        CreateButtonMenu(context, idGenerator, now, messageSentMenuId, "发送消息", "system:message:sent:send", 1, subsystemId, menuIdMap);
        CreateButtonMenu(context, idGenerator, now, messageSentMenuId, "撤回消息", "system:message:sent:recall", 2, subsystemId, menuIdMap);

        // 为所有页面菜单（type=1）添加"页面查看"按钮
        // 勾选"页面查看"即可让菜单显示并访问页面，操作权限独立控制
        // 避免"取消所有操作按钮后菜单消失导致页面无法访问"的问题
        var pageMenuCodes = new[]
        {
            "system:home", "system:user", "system:role", "system:menu", "system:organization",
            "system:tenant", "system:auditLog", "system:config", "system:subsystem", "system:message:sent"
        };
        foreach (var menuCode in pageMenuCodes)
        {
            if (menuIdMap.TryGetValue(menuCode, out var pageMenuId))
            {
                CreateButtonMenu(context, idGenerator, now, pageMenuId, "页面查看", $"{menuCode}:view", 0, subsystemId, menuIdMap);
            }
        }

        return menuIdMap;
    }

    /// <summary>
    /// 创建按钮菜单
    /// </summary>
    private static void CreateButtonMenu(
        SystemDbContext context,
        ISnowflakeIdGenerator idGenerator,
        DateTime now,
        long parentId,
        string name,
        string code,
        int sort,
        long subsystemId,
        Dictionary<string, long> menuIdMap)
    {
        var menuId = idGenerator.NewId();
        menuIdMap[code] = menuId;
        context.Menus.Add(new Menu
        {
            Id = menuId,
            ParentId = parentId,
            Name = name,
            Code = code,
            Path = null,
            Component = null,
            Icon = null,
            Type = (int)MenuType.Button,
            Sort = sort,
            Status = 1,
            IsVisible = true,
            IsCache = false,
            IsAlwaysShow = false,
            PermissionCode = code,
            CreatedTime = now
        });

        // 将按钮分配给子系统
        context.SubsystemMenus.Add(new SubsystemMenu
        {
            SubsystemId = subsystemId,
            MenuId = menuId,
            CreatedTime = now
        });
    }

    private static void CreateDefaultTenants(SystemDbContext context, ISnowflakeIdGenerator idGenerator)
    {
        // 检查是否已存在租户数据
        if (context.Tenants.Any())
        {
            return;
        }

        var now = DateTime.Now;

        // 创建平台租户（超级管理员专属，不参与业务数据隔离）
        // 租户ID=1（固定值，便于识别）
        var platformTenant = new Tenant
        {
            Id = 1,
            Code = "platform",
            Name = "平台租户",
            IsolationLevel = TenantIsolationLevel.Row,
            Status = 1,
            ExpireTime = DateTime.Now.AddYears(100),
            ContactName = "系统管理员",
            ContactEmail = "admin@mes.com",
            Remark = "平台级租户，用于超级管理员",
            CreatedTime = now
        };
        context.Tenants.Add(platformTenant);

        context.SaveChanges();
    }
}

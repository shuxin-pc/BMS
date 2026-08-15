using System.Reflection;
using System.Text.Json;
using Bms.System.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Bms.System.Infrastructure.SeedData;

/// <summary>
/// 测试种子数据加载器
/// <para>从嵌入式 JSON 资源加载 System 服务各表的实际数据，用于测试/开发环境。</para>
/// <para>通过配置开关 <c>SeedTestData:Enabled=true</c> 启用，默认关闭。</para>
/// <para>加载策略：先 TRUNCATE CASCADE 清空目标表，再从 JSON 按外键依赖顺序插入。</para>
/// <para>注意：会覆盖现有数据，仅用于全新测试环境启动，不要在生产环境启用。</para>
/// <para></para>
/// <para>【修改本目录 JSON 必读 - 曾因 Id 冲突踩坑（2026-08），修改前务必阅读】</para>
/// <para>1. 雪花 Id 约为 2e17 量级，超出 JS Number 安全整数范围(9e15)。禁止用 JS JSON.parse</para>
/// <para>   读取后再"生成/取整"Id——会产生可整除尾部的重复 Id，导致 TRUNCATE 重载主键冲突失败</para>
/// <para>   （本次共 41 处重复即源于此，分布在 Menus/SubsystemMenus/RoleMenuAuths 三个文件）。</para>
/// <para>2. Id/ParentId 必须以数据库实际值为准：业务数据、外键、授权均引用这些 Id，手工编造或</para>
/// <para>   改动现有记录 Id 会破坏关联。需修 Id 时以数据库导出值为准，不要用 JS 重新生成。</para>
/// <para>3. 新增菜单必须同步三个文件：Menus.json（菜单本体）、SubsystemMenus.json（菜单-子系统关联）、</para>
/// <para>   RoleMenuAuths.json（角色授权），遗漏会导致菜单不显示或外键/授权缺失。</para>
/// <para>4. 修改后必须验证：以文本保护方式解析 JSON（先把 :(-?\d{16,}) 转字符串再 parse，防精度丢失），</para>
/// <para>   核对各文件无重复 Id、关联表引用的 MenuId 均存在，且与数据库 Id 一致。</para>
/// <para>5. 本 JSON 与 SystemSeedData（代码动态生成）是两条独立路径：SeedTestData 开启时以 JSON 为准；</para>
/// <para>   未开启且全新库时由 SystemSeedData 用雪花 Id 生成。修改菜单权限前先确认当前环境走哪条路径。</para>
/// </summary>
public static class TestSeedData
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// 嵌入式资源路径前缀（对应项目目录 SeedData/TestSeedData/）
    /// </summary>
    private const string ResourcePrefix = "Bms.System.Infrastructure.SeedData.TestSeedData.";

    /// <summary>
    /// 异步加载测试种子数据
    /// </summary>
    /// <param name="context">System 数据库上下文</param>
    /// <param name="logger">日志记录器</param>
    public static async Task InitializeAsync(SystemDbContext context, ILogger logger)
    {
        logger.LogWarning("【测试种子数据】开始加载 System 服务测试数据，将清空现有数据...");

        // TRUNCATE CASCADE 一次性清空所有目标表
        // 自引用表（Menus、Organizations）的 RESTRICT 删除策略不影响 TRUNCATE
        // AuditLogs、Messages、MessageRecipients 无外键到目标表，不受影响
        await context.Database.ExecuteSqlRawAsync(@"
            TRUNCATE TABLE
                ""bms_system"".""RoleMenuAuths"",
                ""bms_system"".""TenantSubsystems"",
                ""bms_system"".""SubsystemMenus"",
                ""bms_system"".""UserRoles"",
                ""bms_system"".""DataPermissions"",
                ""bms_system"".""Users"",
                ""bms_system"".""Menus"",
                ""bms_system"".""Roles"",
                ""bms_system"".""Organizations"",
                ""bms_system"".""Subsystems"",
                ""bms_system"".""Tenants"",
                ""bms_system"".""SystemConfigs""
            CASCADE;
        ");

        logger.LogInformation("【测试种子数据】现有数据已清空，开始从 JSON 资源加载...");

        // 按外键依赖顺序加载（被依赖的表先加载）
        await LoadAsync<Tenant>(context, logger, "Tenants.json");
        await LoadAsync<Subsystem>(context, logger, "Subsystems.json");
        await LoadAsync<Organization>(context, logger, "Organizations.json");
        await LoadAsync<Role>(context, logger, "Roles.json");
        await LoadAsync<DataPermission>(context, logger, "DataPermissions.json");
        await LoadAsync<User>(context, logger, "Users.json");
        await LoadAsync<UserRole>(context, logger, "UserRoles.json");
        await LoadAsync<Menu>(context, logger, "Menus.json");
        await LoadAsync<SubsystemMenu>(context, logger, "SubsystemMenus.json");
        await LoadAsync<TenantSubsystem>(context, logger, "TenantSubsystems.json");
        await LoadAsync<RoleMenuAuth>(context, logger, "RoleMenuAuths.json");
        await LoadAsync<SystemConfig>(context, logger, "SystemConfigs.json");

        await context.SaveChangesAsync();
        logger.LogWarning("【测试种子数据】System 服务测试数据加载完成。");
    }

    /// <summary>
    /// 从嵌入式 JSON 资源加载指定实体类型的数据并加入 DbContext 跟踪
    /// </summary>
    private static async Task LoadAsync<TEntity>(
        SystemDbContext context,
        ILogger logger,
        string resourceName)
        where TEntity : class
    {
        var json = await ReadEmbeddedResourceAsync(resourceName);
        if (string.IsNullOrWhiteSpace(json) || json.Trim() == "[]")
        {
            logger.LogDebug("【测试种子数据】{Resource}: 无数据，跳过", resourceName);
            return;
        }

        var entities = JsonSerializer.Deserialize<List<TEntity>>(json, JsonOptions);
        if (entities == null || entities.Count == 0)
        {
            logger.LogDebug("【测试种子数据】{Resource}: 反序列化后无数据", resourceName);
            return;
        }

        // 清除导航属性引用，避免 EF Core 跟踪到游离实体
        // JSON 中不含导航数据，但集合属性默认初始化为空 List，引用属性为 null，需统一处理
        foreach (var entity in entities)
        {
            ClearNavigationProperties(entity);
        }

        await context.Set<TEntity>().AddRangeAsync(entities);
        logger.LogInformation("【测试种子数据】{Resource}: 已加载 {Count} 条记录", resourceName, entities.Count);
    }

    /// <summary>
    /// 读取嵌入式 JSON 资源内容
    /// </summary>
    private static async Task<string> ReadEmbeddedResourceAsync(string resourceName)
    {
        var fullResourceName = ResourcePrefix + resourceName;
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(fullResourceName)
            ?? throw new FileNotFoundException($"嵌入式资源未找到: {fullResourceName}");
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }

    /// <summary>
    /// 清除实体的导航属性（集合设为 null，引用设为 null）
    /// <para>仅清除非 string 的引用类型属性；值类型和 string 不受影响。</para>
    /// </summary>
    private static void ClearNavigationProperties<TEntity>(TEntity entity)
    {
        if (entity == null)
        {
            return;
        }

        var type = entity.GetType();
        // 仅处理实际类型，跳过 EF Core 代理类型的不同命名空间问题
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite
                && !p.PropertyType.IsValueType
                && p.PropertyType != typeof(string));

        foreach (var prop in properties)
        {
            try
            {
                prop.SetValue(entity, null);
            }
            catch
            {
                // 代理属性或只读属性设值失败时忽略
            }
        }
    }
}

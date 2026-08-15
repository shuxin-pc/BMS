using System.Reflection;
using System.Text.Json;
using Bms.Store.Domain.Entities;
using Bms.Store.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Bms.Store.Application.SeedData;

/// <summary>
/// Store 服务测试种子数据加载器
/// <para>从嵌入式 JSON 资源加载 Store 服务各业务表的实际数据，用于测试/开发环境。</para>
/// <para>通过配置开关 <c>SeedTestData:Enabled=true</c> 启用，默认关闭。</para>
/// <para>加载策略：先 TRUNCATE 清空目标表，再从 JSON 加载。</para>
/// <para>菜单数据不在本加载器处理范围：Store 菜单通过 <see cref="Services.StoreMenuRegistrationService"/>
/// 在启动时调用 System API 自动注册，已包含在 System 服务的 TestSeedData 的 Menus.json 中。</para>
/// <para>注意：会覆盖现有数据，仅用于全新测试环境启动，不要在生产环境启用。</para>
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
    private const string ResourcePrefix = "Bms.Store.Application.SeedData.TestSeedData.";

    /// <summary>
    /// 异步加载测试种子数据
    /// </summary>
    /// <param name="context">Store 数据库上下文</param>
    /// <param name="logger">日志记录器</param>
    public static async Task InitializeAsync(StoreDbContext context, ILogger logger)
    {
        logger.LogWarning("【测试种子数据】开始加载 Store 服务业务数据，将清空现有数据...");

        // TRUNCATE 清空目标表（Stores、UserStores 之间无外键约束，可一次性清空）
        // 注意：Store 数据库其他业务表（Orders、Products 等）不依赖 Stores 的外键约束，
        // 但业务逻辑上可能引用 StoreId；测试环境启动时这些表应为空，TRUNCATE 安全。
        await context.Database.ExecuteSqlRawAsync(@"
            TRUNCATE TABLE
                ""bms_store"".""UserStores"",
                ""bms_store"".""Stores""
            CASCADE;
        ");

        logger.LogInformation("【测试种子数据】现有业务数据已清空，开始从 JSON 资源加载...");

        // 按依赖顺序加载（Stores 先于 UserStores）
        await LoadAsync<Bms.Store.Domain.Entities.Store>(context, logger, "Stores.json");
        await LoadAsync<UserStore>(context, logger, "UserStores.json");

        await context.SaveChangesAsync();
        logger.LogWarning("【测试种子数据】Store 服务业务数据加载完成。");
    }

    /// <summary>
    /// 从嵌入式 JSON 资源加载指定实体类型的数据并加入 DbContext 跟踪
    /// </summary>
    private static async Task LoadAsync<TEntity>(
        StoreDbContext context,
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

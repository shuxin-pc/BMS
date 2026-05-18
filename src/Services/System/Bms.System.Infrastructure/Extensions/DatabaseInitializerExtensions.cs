using Bms.BuildingBlocks.Core.IdGenerator;
using Bms.System.Infrastructure.SeedData;
using Bms.System.Domain.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Bms.System.Infrastructure.Extensions;

/// <summary>
/// 数据库初始化扩展方法
/// </summary>
public static class DatabaseInitializerExtensions
{
    /// <summary>
    /// 初始化系统数据库
    /// </summary>
    public static IApplicationBuilder UseSystemDatabaseInitializer(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<SystemDbContext>();
        var idGenerator = scope.ServiceProvider.GetRequiredService<ISnowflakeIdGenerator>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        // 确保数据库创建（如果不存在）
        context.Database.EnsureCreated();

        // 初始化种子数据
        SystemSeedData.Initialize(context, idGenerator, passwordHasher);

        // 初始化系统配置（独立于用户数据，确保配置项存在）
        SystemSeedData.InitializeSystemConfigs(context);

        return app;
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Bms.Identity.Api.Data;

/// <summary>
/// IdentityDbContext 设计时工厂（用于 EF Core 迁移）
/// </summary>
public class IdentityDbContextFactory : IDesignTimeDbContextFactory<IdentityDbContext>
{
    public IdentityDbContext CreateDbContext(string[] args)
    {
        // 获取项目根目录
        var basePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "Bms.Identity.Api");
        if (!File.Exists(Path.Combine(basePath, "appsettings.json")))
        {
            basePath = Directory.GetCurrentDirectory();
        }

        // 构建配置
        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        // 创建 DbContextOptions
        var optionsBuilder = new DbContextOptionsBuilder<IdentityDbContext>();
        var connectionString = configuration.GetConnectionString("IdentityDb");

        optionsBuilder.UseNpgsql(connectionString);
        // 与 Program.cs 运行时配置保持一致，确保设计时模型与运行时模型相同
        optionsBuilder.UseOpenIddict();

        return new IdentityDbContext(optionsBuilder.Options);
    }
}

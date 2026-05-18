using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Bms.System.Infrastructure;

/// <summary>
/// 设计时DbContext工厂，用于EF Core迁移
/// </summary>
public class SystemDbContextFactory : IDesignTimeDbContextFactory<SystemDbContext>
{
    public SystemDbContext CreateDbContext(string[] args)
    {
        // 创建临时配置
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<SystemDbContext>();
        var connectionString = configuration.GetConnectionString("SystemDb")
            ?? "Host=localhost;Database=bms_system;Username=postgres;Password=postgres";

        optionsBuilder.UseNpgsql(connectionString);

        return new SystemDbContext(optionsBuilder.Options);
    }
}

using Bms.BuildingBlocks.Core.Attributes;
using Bms.BuildingBlocks.Core.IdGenerator;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Bms.BuildingBlocks.Core.Extensions;

/// <summary>
/// 雪花ID生成器扩展方法
/// </summary>
public static class IdGeneratorExtensions
{
    /// <summary>
    /// 添加雪花ID生成器
    /// </summary>
    public static IServiceCollection AddSnowflakeIdGenerator(this IServiceCollection services, IConfiguration configuration)
    {
        var workerId = configuration.GetValue<long>("Snowflake:WorkerId", 1L);

        services.AddSingleton<ISnowflakeIdGenerator>(new SnowflakeIdGenerator(workerId));

        return services;
    }
}

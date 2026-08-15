using Bms.BuildingBlocks.Storage.Abstractions;
using Bms.BuildingBlocks.Storage.Internal;
using Bms.BuildingBlocks.Storage.Minio;
using Bms.BuildingBlocks.Storage.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Bms.BuildingBlocks.Storage.Extensions;

/// <summary>
/// 对象存储注册扩展
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 一站式注册对象存储能力：绑定 ObjectStorage 配置节，注册上传、读取解析、外链校验、
    /// 孤儿清理与底层存储，并在服务启动时确保桶存在。
    /// 依赖 ISnowflakeIdGenerator（用于生成对象文件名），需同时调用 AddSnowflakeIdGenerator
    /// </summary>
    public static IServiceCollection AddObjectStorage(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ObjectStorageOptions>(configuration.GetSection(ObjectStorageOptions.SectionName));

        // 注册具体类型再转发接口，使桶初始化任务能复用同一实例（S3 客户端只需构造一次）
        services.AddSingleton<MinioFileStorageService>();
        services.AddSingleton<IFileStorageService>(sp => sp.GetRequiredService<MinioFileStorageService>());

        services.AddSingleton<ImageFileValidator>();
        services.AddSingleton<IExternalUrlValidator, ExternalUrlValidator>();
        services.AddSingleton<IFileUploadService, FileUploadService>();
        services.AddSingleton<IFileReferenceResolver, FileReferenceResolver>();
        services.AddSingleton<IOrphanObjectCleaner, OrphanObjectCleaner>();

        services.AddHostedService<BucketInitializer>();

        return services;
    }
}

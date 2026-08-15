using Bms.BuildingBlocks.Storage.Minio;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Bms.BuildingBlocks.Storage.Internal;

/// <summary>
/// 启动时确保对象存储桶存在。
/// 做成后台启动任务而非在存储服务构造函数内执行，是为了避免对象存储不可用时拖死服务启动；
/// 失败仅记日志不阻断启动，与 Program.cs 中数据库初始化失败的处理方式保持一致
/// </summary>
internal sealed class BucketInitializer : IHostedService
{
    private readonly MinioFileStorageService _storage;
    private readonly ILogger<BucketInitializer> _logger;

    public BucketInitializer(MinioFileStorageService storage, ILogger<BucketInitializer> logger)
    {
        _storage = storage;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _storage.EnsureBucketAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "对象存储桶初始化失败，图片上传与读取功能将不可用");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

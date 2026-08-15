using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;
using Bms.BuildingBlocks.Storage.Abstractions;
using Bms.BuildingBlocks.Storage.Internal;
using Bms.BuildingBlocks.Storage.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bms.BuildingBlocks.Storage.Minio;

/// <summary>
/// 基于 AWSSDK.S3 的对象存储实现。
/// 选用 S3 SDK 而非 MinIO 专用 SDK，是为了将来切换到阿里云 OSS / 腾讯云 COS 时只改配置不改代码
/// </summary>
internal sealed class MinioFileStorageService : IFileStorageService
{
    private readonly ObjectStorageOptions _options;
    private readonly ILogger<MinioFileStorageService> _logger;
    private readonly IAmazonS3 _client;
    private readonly IAmazonS3 _presignClient;
    private readonly Protocol _presignProtocol;

    public MinioFileStorageService(IOptions<ObjectStorageOptions> options, ILogger<MinioFileStorageService> logger)
    {
        _options = options.Value;
        _logger = logger;
        _client = CreateClient(_options.Endpoint, _options);

        // 预签名 URL 由浏览器访问，必须用浏览器可达的 PublicEndpoint 签发；
        // 两者相同时复用同一客户端，避免无意义的实例
        _presignClient = string.Equals(_options.PublicEndpoint, _options.Endpoint, StringComparison.OrdinalIgnoreCase)
            ? _client
            : CreateClient(_options.PublicEndpoint, _options);

        // SDK 签发 URL 的协议只认 GetPreSignedUrlRequest.Protocol（默认 HTTPS），不读 ServiceURL 的 scheme，
        // 不按 PublicEndpoint 对齐会让 http 的 MinIO 签出 https 链接，浏览器访问报 SSL 错误
        _presignProtocol = _options.PublicEndpoint.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            ? Protocol.HTTP
            : Protocol.HTTPS;
    }

    /// <inheritdoc />
    public async Task UploadAsync(string objectKey, Stream content, string contentType, long length, CancellationToken cancellationToken = default)
    {
        var request = new PutObjectRequest
        {
            BucketName = _options.BucketName,
            Key = objectKey,
            InputStream = content,
            ContentType = contentType,
            // 流由调用方（Controller）负责释放，SDK 不应擅自关闭
            AutoCloseStream = false
        };
        request.Headers.ContentLength = length;

        await _client.PutObjectAsync(request, cancellationToken);
    }

    /// <inheritdoc />
    public string? GetPresignedUrl(string objectKey, PresignScope scope)
    {
        if (!IsInScope(objectKey, scope))
        {
            _logger.LogWarning("拒绝为越权对象签名，objectKey={ObjectKey}", objectKey);
            return null;
        }

        return _presignClient.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = _options.BucketName,
            Key = objectKey,
            Verb = HttpVerb.GET,
            Protocol = _presignProtocol,
            Expires = DateTime.UtcNow.AddMinutes(_options.PresignExpireMinutes)
        });
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string> GetPresignedUrls(IEnumerable<string> objectKeys, PresignScope scope)
    {
        var urls = new Dictionary<string, string>();

        foreach (var objectKey in objectKeys.Distinct())
        {
            var url = GetPresignedUrl(objectKey, scope);
            if (url is not null)
            {
                urls[objectKey] = url;
            }
        }

        return urls;
    }

    /// <inheritdoc />
    public async Task DeleteAsync(string objectKey, PresignScope scope, CancellationToken cancellationToken = default)
    {
        // 外链与越权 key 在此静默跳过：外链不匹配租户/门店前缀，因此不会被误删
        if (!IsInScope(objectKey, scope))
        {
            return;
        }

        try
        {
            await _client.DeleteObjectAsync(_options.BucketName, objectKey, cancellationToken);
        }
        catch (Exception ex)
        {
            // 数据库事务已提交，此处失败无法回滚，只能记日志并交由孤儿清理任务兜底
            _logger.LogError(ex, "删除对象存储文件失败，objectKey={ObjectKey}", objectKey);
        }
    }

    /// <inheritdoc />
    public async Task DeleteManyAsync(IEnumerable<string> objectKeys, PresignScope scope, CancellationToken cancellationToken = default)
    {
        var deletableKeys = objectKeys
            .Where(objectKey => IsInScope(objectKey, scope))
            .Distinct()
            .ToList();

        if (deletableKeys.Count == 0)
        {
            return;
        }

        // S3 批量删除接口单次上限 1000 个对象
        foreach (var batch in deletableKeys.Chunk(1000))
        {
            try
            {
                await _client.DeleteObjectsAsync(new DeleteObjectsRequest
                {
                    BucketName = _options.BucketName,
                    Objects = batch.Select(objectKey => new KeyVersion { Key = objectKey }).ToList()
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量删除对象存储文件失败，本批 {Count} 个", batch.Length);
            }
        }
    }

    /// <summary>
    /// 确保桶存在。新建的桶默认为 private，符合「所有访问必须持有效签名」的安全要求
    /// </summary>
    public async Task EnsureBucketAsync(CancellationToken cancellationToken = default)
    {
        if (await AmazonS3Util.DoesS3BucketExistV2Async(_client, _options.BucketName))
        {
            return;
        }

        await _client.PutBucketAsync(new PutBucketRequest { BucketName = _options.BucketName }, cancellationToken);
        _logger.LogInformation("已创建对象存储桶 {BucketName}", _options.BucketName);
    }

    /// <summary>
    /// 列举桶内全部对象。
    /// 仅供孤儿清理任务使用，故不放进 IFileStorageService —— 业务代码没有遍历全桶的正当需求，
    /// 暴露出去反而成了绕过租户前缀隔离的读取入口
    /// </summary>
    public async Task<IReadOnlyList<StoredObject>> ListAllObjectsAsync(CancellationToken cancellationToken = default)
    {
        var objects = new List<StoredObject>();
        string? continuationToken = null;

        do
        {
            var response = await _client.ListObjectsV2Async(new ListObjectsV2Request
            {
                BucketName = _options.BucketName,
                ContinuationToken = continuationToken
            }, cancellationToken);

            objects.AddRange(response.S3Objects.Select(o => new StoredObject(o.Key, ToUtc(o.LastModified))));

            // 单次响应上限 1000 个对象，未取完时继续翻页
            continuationToken = response.IsTruncated ? response.NextContinuationToken : null;
        }
        while (continuationToken is not null);

        return objects;
    }

    /// <summary>
    /// 删除对象，不做 scope 前缀校验。
    /// 仅供孤儿清理任务使用：清理是跨租户的运维动作，不存在单一 PresignScope 可传。
    /// 同样不放进 IFileStorageService，以免业务代码取得无授权约束的删除能力
    /// </summary>
    /// <returns>实际删除成功的对象数</returns>
    public async Task<int> DeleteWithoutScopeAsync(IReadOnlyCollection<string> objectKeys, CancellationToken cancellationToken = default)
    {
        var deletedCount = 0;

        // S3 批量删除接口单次上限 1000 个对象
        foreach (var batch in objectKeys.Chunk(1000))
        {
            try
            {
                var response = await _client.DeleteObjectsAsync(new DeleteObjectsRequest
                {
                    BucketName = _options.BucketName,
                    Objects = batch.Select(objectKey => new KeyVersion { Key = objectKey }).ToList()
                }, cancellationToken);

                deletedCount += response.DeletedObjects.Count;
            }
            catch (Exception ex)
            {
                // 清理任务是兜底手段，本批失败下次执行会重新识别为孤儿，无需中断整体流程
                _logger.LogError(ex, "清理孤儿对象失败，本批 {Count} 个", batch.Length);
            }
        }

        return deletedCount;
    }

    /// <summary>
    /// 把 S3 返回的时间规范化为 UTC。
    /// Kind 未标注时按 UTC 解读（S3 协议返回的本就是 UTC）：若误当本地时间换算，
    /// 对象会显得比实际更旧，可能在保留期未满时就被清理掉
    /// </summary>
    private static DateTime ToUtc(DateTime value)
        => value.Kind == DateTimeKind.Local
            ? value.ToUniversalTime()
            : DateTime.SpecifyKind(value, DateTimeKind.Utc);

    /// <summary>
    /// 校验 objectKey 是否落在授权范围的前缀内。这是租户/门店越权隔离的实际执行点
    /// </summary>
    private static bool IsInScope(string objectKey, PresignScope scope)
        => !string.IsNullOrWhiteSpace(objectKey)
           && objectKey.StartsWith(ObjectKeyBuilder.BuildScopePrefix(scope.TenantId, scope.StoreId), StringComparison.Ordinal);

    /// <summary>
    /// 创建 S3 客户端。MinIO 不支持 virtual-host 风格的桶寻址，必须启用 path style；
    /// 使用自定义 ServiceURL 时 SDK 无法推断区域，需显式指定签名区域
    /// </summary>
    private static IAmazonS3 CreateClient(string endpoint, ObjectStorageOptions options)
        => new AmazonS3Client(
            new BasicAWSCredentials(options.AccessKey, options.SecretKey),
            new AmazonS3Config
            {
                ServiceURL = endpoint,
                ForcePathStyle = true,
                AuthenticationRegion = "us-east-1"
            });
}

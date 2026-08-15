using Bms.BuildingBlocks.Storage.Abstractions;
using Bms.BuildingBlocks.Storage.Minio;
using Microsoft.Extensions.Logging;

namespace Bms.BuildingBlocks.Storage.Internal;

/// <summary>
/// 孤儿对象清理实现。
/// 判定逻辑集中在此，避免各服务自行写差集比对——写错一次即造成图片永久丢失
/// </summary>
internal sealed class OrphanObjectCleaner : IOrphanObjectCleaner
{
    private readonly MinioFileStorageService _storage;
    private readonly ILogger<OrphanObjectCleaner> _logger;

    public OrphanObjectCleaner(MinioFileStorageService storage, ILogger<OrphanObjectCleaner> logger)
    {
        _storage = storage;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<OrphanCleanupResult> CleanAsync(OrphanCleanupRequest request, CancellationToken cancellationToken = default)
    {
        // 必须先列举、后查库：此刻之后才建立的引用一定会出现在随后查出的引用集合中，因而不会被误删。
        // 反序则会把两个动作之间新上传并保存的对象判成孤儿
        var objects = await _storage.ListAllObjectsAsync(cancellationToken);
        if (objects.Count == 0)
        {
            return new OrphanCleanupResult();
        }

        var referencedKeys = await request.LoadReferencedKeysAsync(cancellationToken);
        var retentionDeadline = DateTime.UtcNow - request.RetentionPeriod;

        var orphanKeys = objects
            .Where(o => o.LastModifiedUtc < retentionDeadline)
            .Where(o => ObjectKeyBuilder.TryExtractBizType(o.Key, out var bizType) && request.BizTypes.Contains(bizType))
            .Select(o => o.Key)
            .Where(key => !referencedKeys.Contains(key))
            .ToList();

        if (orphanKeys.Count == 0)
        {
            return new OrphanCleanupResult { ScannedCount = objects.Count };
        }

        // 逐个记录：删除不可逆，出问题时日志是唯一的追溯依据
        foreach (var orphanKey in orphanKeys)
        {
            _logger.LogInformation("识别到孤儿对象，准备删除 {ObjectKey}", orphanKey);
        }

        var deletedCount = await _storage.DeleteWithoutScopeAsync(orphanKeys, cancellationToken);

        return new OrphanCleanupResult
        {
            ScannedCount = objects.Count,
            DeletedCount = deletedCount
        };
    }
}

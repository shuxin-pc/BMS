namespace Bms.BuildingBlocks.Storage.Abstractions;

/// <summary>
/// 孤儿清理结果，供调用方记录运维日志
/// </summary>
public sealed class OrphanCleanupResult
{
    /// <summary>
    /// 桶内扫描到的对象总数
    /// </summary>
    public int ScannedCount { get; init; }

    /// <summary>
    /// 实际删除的孤儿对象数
    /// </summary>
    public int DeletedCount { get; init; }
}

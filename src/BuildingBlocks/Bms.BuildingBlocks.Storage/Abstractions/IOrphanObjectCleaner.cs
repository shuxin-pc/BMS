namespace Bms.BuildingBlocks.Storage.Abstractions;

/// <summary>
/// 孤儿对象清理入口。
/// 孤儿的两个来源：用户上传后未提交表单，以及业务删除时对象删除失败（删库成功、删对象失败，
/// 该失败被有意降级为仅记日志，就是为了由本任务兜底）。
///
/// 「先列举对象、后查数据库引用」的定序是本能力的安全关键，因此封装在类库内，
/// 调用方只提供查库回调，无从把顺序写反
/// </summary>
public interface IOrphanObjectCleaner
{
    /// <summary>
    /// 执行一轮清理。判定与删除均在类库内完成，调用方不需要（也拿不到）裸删除能力
    /// </summary>
    Task<OrphanCleanupResult> CleanAsync(OrphanCleanupRequest request, CancellationToken cancellationToken = default);
}

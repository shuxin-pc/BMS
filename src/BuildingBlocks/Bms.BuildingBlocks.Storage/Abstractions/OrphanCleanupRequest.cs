namespace Bms.BuildingBlocks.Storage.Abstractions;

/// <summary>
/// 孤儿清理请求。
/// 三项参数各自对应一重误删防护，均为必填
/// </summary>
public sealed class OrphanCleanupRequest
{
    /// <summary>
    /// 参与清理的业务类型。桶为多服务、多业务共用，只有引用已被
    /// <see cref="LoadReferencedKeysAsync"/> 统计在内的 bizType 才可列入。
    /// 漏列的后果只是该类型对象不被回收（安全失败）；错列则会删掉仍在使用的对象
    /// </summary>
    public required IReadOnlyCollection<string> BizTypes { get; init; }

    /// <summary>
    /// 保留期。上传时间距今不足此时长的对象不清理，
    /// 用于保护「已上传、但用户仍停留在编辑界面尚未提交」的对象
    /// </summary>
    public required TimeSpan RetentionPeriod { get; init; }

    /// <summary>
    /// 加载数据库中在用的引用集合。集合内混有外链无妨——外链不会命中桶内对象键。
    ///
    /// 设计成回调而非直接传入集合，是为了由类库掌握「先列举对象、后查数据库」的顺序：
    /// 若先查库再列举，两个动作之间新上传并保存的对象会被误判为孤儿
    /// </summary>
    public required Func<CancellationToken, Task<ISet<string>>> LoadReferencedKeysAsync { get; init; }
}

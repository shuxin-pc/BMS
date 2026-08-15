namespace Bms.BuildingBlocks.Storage.Abstractions;

/// <summary>
/// 底层对象存储抽象。
/// 预签名与删除均要求传入 PresignScope，前缀越权校验因此无法被漏写。
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// 上传对象。objectKey 由 IFileUploadService 拼装，不应由业务代码自行构造
    /// </summary>
    Task UploadAsync(string objectKey, Stream content, string contentType, long length, CancellationToken cancellationToken = default);

    /// <summary>
    /// 为单个 objectKey 生成预签名下载地址。
    /// key 前缀与 scope 不匹配时返回 null。签名是纯本地 HMAC 计算，不产生网络请求
    /// </summary>
    string? GetPresignedUrl(string objectKey, PresignScope scope);

    /// <summary>
    /// 批量生成预签名地址，供列表接口使用。
    /// 返回 objectKey 到 URL 的映射，前缀越权的 key 不会出现在结果中
    /// </summary>
    IReadOnlyDictionary<string, string> GetPresignedUrls(IEnumerable<string> objectKeys, PresignScope scope);

    /// <summary>
    /// 删除单个对象。前缀与 scope 不匹配则跳过；删除失败仅记日志不抛异常，
    /// 残留对象由孤儿清理任务兜底 —— 宁可留孤儿文件，不可因删文件失败而回滚业务数据
    /// </summary>
    Task DeleteAsync(string objectKey, PresignScope scope, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量删除对象。语义同 DeleteAsync
    /// </summary>
    Task DeleteManyAsync(IEnumerable<string> objectKeys, PresignScope scope, CancellationToken cancellationToken = default);
}

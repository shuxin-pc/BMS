namespace Bms.BuildingBlocks.Storage.Abstractions;

/// <summary>
/// 文件上传编排入口。这是接入服务唯一需要调用的上传 API。
/// 校验、objectKey 拼装、上传全部在实现内部完成，接入方无需（也无法）重复这些逻辑。
/// </summary>
public interface IFileUploadService
{
    /// <summary>
    /// 上传单个文件。校验失败不抛异常，通过返回值的 Succeeded / ErrorMessage 表达
    /// </summary>
    Task<FileUploadOutcome> UploadAsync(FileUploadRequest request, CancellationToken cancellationToken = default);
}

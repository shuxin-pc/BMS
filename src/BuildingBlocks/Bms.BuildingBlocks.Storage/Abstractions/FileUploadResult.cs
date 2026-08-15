namespace Bms.BuildingBlocks.Storage.Abstractions;

/// <summary>
/// 文件上传成功后的结果
/// </summary>
public sealed class FileUploadResult
{
    /// <summary>
    /// 对象键。这是需要入库保存的值，不要保存完整 URL
    /// </summary>
    public required string ObjectKey { get; init; }

    /// <summary>
    /// 客户端原始文件名，供前端回显
    /// </summary>
    public required string FileName { get; init; }

    /// <summary>
    /// 文件字节数
    /// </summary>
    public required long Size { get; init; }
}

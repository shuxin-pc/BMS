namespace Bms.BuildingBlocks.Storage.Abstractions;

/// <summary>
/// 文件上传入参。
/// 刻意用 Stream 而非 IFormFile，使本类库无需依赖 ASP.NET Core，
/// 从而可被各服务的 Application 层直接引用而不被动引入 Web 依赖。
/// </summary>
public sealed class FileUploadRequest
{
    /// <summary>
    /// 文件内容流。必须支持 Seek —— 魔术字节校验会读取流开头，之后需复位到起点再上传
    /// </summary>
    public required Stream Content { get; init; }

    /// <summary>
    /// 客户端原始文件名。仅用于提取扩展名与前端回显，不参与 objectKey 拼装
    /// </summary>
    public required string FileName { get; init; }

    /// <summary>
    /// 客户端声明的 MIME 类型。仅作辅助校验，真实类型以魔术字节为准
    /// </summary>
    public required string ContentType { get; init; }

    /// <summary>
    /// 文件字节数
    /// </summary>
    public required long Length { get; init; }

    /// <summary>
    /// 业务类型，需命中配置的 AllowedBizTypes 白名单。作为 objectKey 的一级目录
    /// </summary>
    public required string BizType { get; init; }

    /// <summary>
    /// 租户ID。应取自 ICurrentUser，不接受前端传入
    /// </summary>
    public required long TenantId { get; init; }

    /// <summary>
    /// 门店ID。无门店维度的服务传 null
    /// </summary>
    public long? StoreId { get; init; }
}

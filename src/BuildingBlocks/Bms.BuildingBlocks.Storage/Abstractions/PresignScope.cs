namespace Bms.BuildingBlocks.Storage.Abstractions;

/// <summary>
/// 对象访问的授权范围。
/// 作为预签名与删除操作的必填参数，用于校验 objectKey 前缀是否属于当前租户/门店。
/// 设计成必填而非可选，是为了让前缀越权校验不可能被调用方漏写。
/// </summary>
public sealed class PresignScope
{
    /// <summary>
    /// 租户ID。应取自 ICurrentUser，不接受前端传入
    /// </summary>
    public required long TenantId { get; init; }

    /// <summary>
    /// 门店ID。为 null 时授权范围放宽至整个租户，供无门店维度的服务（如用户头像）使用
    /// </summary>
    public long? StoreId { get; init; }
}

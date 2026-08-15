namespace Bms.BuildingBlocks.Storage.Internal;

/// <summary>
/// objectKey 拼装（纯函数）。
/// 完全由服务端拼装，不拼接任何未经校验的客户端字符串，杜绝路径穿越
/// </summary>
internal static class ObjectKeyBuilder
{
    /// <summary>
    /// 拼装 objectKey：{tenantId}/{storeId}/{bizType}/{yyyyMM}/{fileId}{extension}。
    /// 无门店维度时退化为 {tenantId}/{bizType}/{yyyyMM}/{fileId}{extension}。
    /// 用雪花 ID 而非客户端原始文件名，避免路径穿越与中文编码问题
    /// </summary>
    public static string Build(long tenantId, long? storeId, string bizType, string extension, long fileId)
        => $"{BuildScopePrefix(tenantId, storeId)}{bizType}/{DateTime.Now:yyyyMM}/{fileId}{extension}";

    /// <summary>
    /// 授权范围前缀。预签名与删除操作用它校验 key 归属，是租户/门店隔离的实际执行点
    /// </summary>
    public static string BuildScopePrefix(long tenantId, long? storeId)
        => storeId.HasValue ? $"{tenantId}/{storeId.Value}/" : $"{tenantId}/";

    /// <summary>
    /// 从 objectKey 反解 bizType，供孤儿清理判定对象归属。
    /// 段数不符合 Build 产出的两种形态时返回 false —— 形态未知的对象一律不参与清理，
    /// 宁可漏清理，不可误删他人对象
    /// </summary>
    public static bool TryExtractBizType(string objectKey, out string bizType)
    {
        var segments = objectKey.Split('/');
        bizType = segments.Length switch
        {
            // {tenantId}/{storeId}/{bizType}/{yyyyMM}/{fileId}{ext}
            5 => segments[2],
            // {tenantId}/{bizType}/{yyyyMM}/{fileId}{ext}（无门店维度）
            4 => segments[1],
            _ => string.Empty
        };

        return bizType.Length > 0;
    }
}

namespace Bms.BuildingBlocks.Core.Search;

/// <summary>
/// 全局搜索统一请求契约（各子系统 /search 端点共用）
/// </summary>
public class GlobalSearchRequestDto
{
    /// <summary>
    /// 搜索关键字（服务端校验：Trim 后长度 ≥ 1 且 ≤ 50）
    /// </summary>
    public string Keyword { get; set; } = string.Empty;

    /// <summary>
    /// 每组返回条数上限，默认 5，最大 10
    /// </summary>
    public int Limit { get; set; } = 5;
}

/// <summary>
/// 全局搜索结果项
/// </summary>
public class SearchResultItemDto
{
    /// <summary>
    /// 主标题（顾客姓名 / 单号 / 菜单名等）
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 辅助信息（手机号 / 金额 / 日期等）
    /// </summary>
    public string? Subtitle { get; set; }
}

/// <summary>
/// 全局搜索结果分组（仅返回有命中的分组）
/// </summary>
public class SearchResultGroupDto
{
    /// <summary>
    /// 分组名：顾客 / 订单 / 商品 / 项目卡 / 储值 / 预约 / 用户（前端据此映射跳转路由）
    /// </summary>
    public string Group { get; set; } = string.Empty;

    /// <summary>
    /// 该分组下的命中条目
    /// </summary>
    public List<SearchResultItemDto> Items { get; set; } = new();
}

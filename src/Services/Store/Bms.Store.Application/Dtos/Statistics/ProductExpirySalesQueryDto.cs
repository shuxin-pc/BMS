namespace Bms.Store.Application.Dtos.Statistics;

/// <summary>
/// 效期销售统计查询参数（B5.5）
/// </summary>
/// <remarks>
/// 按商品 + 效期区间聚合统计销售/消耗情况，支持商品类型筛选（实物商品/耗材）。
/// 数据源：OrderItemBatch 表（不含已退款数量）。
/// </remarks>
public class ProductExpirySalesQueryDto : Bms.Store.Application.Dtos.PagedRequestDto
{
    /// <summary>
    /// 开始日期（默认本月1日，含当天）
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// 结束日期（默认今日，含当天）
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// 商品类型筛选：1=实物商品（零售出库），3=耗材（服务耗材出库），null=全部
    /// </summary>
    public int? ProductType { get; set; }

    /// <summary>
    /// 商品分类ID（可选）
    /// </summary>
    public long? CategoryId { get; set; }

    /// <summary>
    /// 效期区间筛选（1=已过期, 2=7天内, 3=30天内, 4=90天内, 5=90天以上），null=全部区间
    /// </summary>
    public int? ExpiryBucket { get; set; }

    /// <summary>
    /// 关键词（商品名称/编码模糊匹配，可选）
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// 门店ID（多门店场景，默认当前门店）
    /// </summary>
    public long? StoreId { get; set; }

    /// <summary>
    /// 排序字段：quantity=销售数量(默认)，amount=销售金额，count=销售笔数
    /// </summary>
    public string SortBy { get; set; } = "quantity";
}

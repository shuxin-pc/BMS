namespace Bms.Store.Application.Dtos.Statistics;

/// <summary>
/// 效期销售统计结果项（B5.5）
/// </summary>
/// <remarks>
/// 表示某个商品在某个效期区间内的销售/消耗统计。
/// 一行 = 一个商品 × 一个效期区间。
/// </remarks>
public class ProductExpirySalesStatDto
{
    /// <summary>
    /// 商品ID
    /// </summary>
    public long ProductId { get; set; }

    /// <summary>
    /// 商品名称
    /// </summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// 商品编码
    /// </summary>
    public string ProductCode { get; set; } = string.Empty;

    /// <summary>
    /// 商品类型：1=实物商品，3=耗材
    /// </summary>
    public int ProductType { get; set; }

    /// <summary>
    /// 商品类型名称
    /// </summary>
    public string ProductTypeName { get; set; } = string.Empty;

    /// <summary>
    /// 商品分类ID
    /// </summary>
    public long? CategoryId { get; set; }

    /// <summary>
    /// 商品分类名称
    /// </summary>
    public string? CategoryName { get; set; }

    /// <summary>
    /// 效期区间编号：1=已过期, 2=7天内, 3=30天内, 4=90天内, 5=90天以上
    /// </summary>
    public int ExpiryBucket { get; set; }

    /// <summary>
    /// 效期区间名称
    /// </summary>
    public string ExpiryBucketName { get; set; } = string.Empty;

    /// <summary>
    /// 批次过期日期范围（仅展示用，区间内最早/最晚效期）
    /// </summary>
    public DateTime? ExpirationDateFrom { get; set; }

    public DateTime? ExpirationDateTo { get; set; }

    /// <summary>
    /// 销售数量（已扣除退款数量 = Sum(Quantity - RefundedQuantity)）
    /// </summary>
    public decimal SalesQuantity { get; set; }

    /// <summary>
    /// 销售金额（按 OrderItem.Price 计算，已扣除退款比例）
    /// </summary>
    public decimal SalesAmount { get; set; }

    /// <summary>
    /// 销售笔数（订单数）
    /// </summary>
    public int OrderCount { get; set; }

    /// <summary>
    /// 占该商品总销售数量的百分比
    /// </summary>
    public decimal QuantityPercentage { get; set; }
}

namespace Bms.Store.Application.Dtos.PriceChangeLogs;

/// <summary>
/// 批量调价输入 DTO
/// 支持按商品ID列表 / 商品分类 / 供应商 / 全部商品进行批量调价
/// 调价方式支持百分比、固定金额增减、设置为新值
/// </summary>
public class BatchPriceAdjustDto
{
    /// <summary>调价范围类型：1=商品ID列表 2=商品分类 3=供应商 4=全部商品</summary>
    public int RangeType { get; set; } = 1;

    /// <summary>商品ID列表（RangeType=1 时使用）</summary>
    public List<long>? ProductIds { get; set; }

    /// <summary>商品分类ID（RangeType=2 时使用）</summary>
    public long? ProductCategoryId { get; set; }

    /// <summary>供应商ID（RangeType=3 时使用）</summary>
    public long? SupplierId { get; set; }

    /// <summary>调价方式：1=百分比 2=固定金额增减 3=设置为新值</summary>
    public int AdjustType { get; set; } = 1;

    /// <summary>
    /// 调价值
    /// AdjustType=1 时为百分比（如 10 表示 +10%，-10 表示 -10%）
    /// AdjustType=2 时为金额增减（如 5 表示 +5 元，-5 表示 -5 元）
    /// AdjustType=3 时为新的价格值
    /// </summary>
    public decimal AdjustValue { get; set; }

    /// <summary>调价备注</summary>
    public string? Remark { get; set; }
}

/// <summary>
/// 批量调价结果 DTO
/// </summary>
public class BatchPriceAdjustResultDto
{
    /// <summary>成功调价数量</summary>
    public int SuccessCount { get; set; }

    /// <summary>失败数量（如计算后新价格为负等）</summary>
    public int FailedCount { get; set; }

    /// <summary>跳过数量（原价格与新价格一致）</summary>
    public int SkippedCount { get; set; }

    /// <summary>失败明细</summary>
    public List<BatchPriceAdjustFailureItem> Failures { get; set; } = new();
}

/// <summary>
/// 批量调价失败明细
/// </summary>
public class BatchPriceAdjustFailureItem
{
    /// <summary>商品ID</summary>
    public long ProductId { get; set; }

    /// <summary>商品名称</summary>
    public string? ProductName { get; set; }

    /// <summary>失败原因</summary>
    public string Reason { get; set; } = string.Empty;
}

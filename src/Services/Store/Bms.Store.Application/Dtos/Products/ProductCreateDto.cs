namespace Bms.Store.Application.Dtos.Products;

/// <summary>
/// 创建门店商品档案输入 DTO（仅 Store 字段 + MasterId）
/// Master 字段（编码/名称/类型等）由 ProductMasterAppService 管理
/// 服务商品子表字段归 Master 层，此处不含
/// </summary>
public class ProductCreateDto
{
    /// <summary>关联商品主档ID（必填，需先创建主档）</summary>
    public long MasterId { get; set; }

    /// <summary>零售价（分店独立定价）</summary>
    public decimal Price { get; set; }

    /// <summary>成本价（分店独立）</summary>
    public decimal? CostPrice { get; set; }

    /// <summary>低库存预警阈值（null=不预警）</summary>
    public decimal? LowStockThreshold { get; set; }

    /// <summary>效期预警天数（null=不预警）</summary>
    public int? ExpiryAlertDays { get; set; }

    /// <summary>积压预警阈值（null=不预警）</summary>
    public decimal? OverstockThreshold { get; set; }

    /// <summary>上架状态（1:上架 2:下架）</summary>
    public int Status { get; set; }

    /// <summary>分店级备注</summary>
    public string? Remark { get; set; }
}

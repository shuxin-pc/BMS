namespace Bms.Store.Application.Dtos.Products;

/// <summary>
/// 统一配置门店档案 Store 字段 DTO（对应设计文档 7.2 节单选/多选统一逻辑）
/// 将输入的 Store 字段值应用到选中门店：覆盖已存在 Product + 自动创建缺失 Product
/// 单选即"编辑某门店"，多选即"统一配置"，逻辑一致（设计文档 7.2 节）
/// </summary>
public class ProductStoreBatchConfigDto
{
    /// <summary>商品主档ID</summary>
    public long MasterId { get; set; }

    /// <summary>选中门店ID列表（支持单选/多选，逻辑一致）</summary>
    public List<long> StoreIds { get; set; } = new();

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

    /// <summary>上架状态（1:上架 2:下架，分店选择性上架）</summary>
    public int Status { get; set; } = 1;

    /// <summary>分店级备注</summary>
    public string? Remark { get; set; }
}

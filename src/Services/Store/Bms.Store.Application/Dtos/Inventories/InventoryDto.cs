namespace Bms.Store.Application.Dtos.Inventories;

/// <summary>
/// 库存输出 DTO
/// 以档案为主表左连接库存汇总表，档案存在即可见，无库存记录时数量为 0
/// </summary>
public class InventoryDto
{
    /// <summary>
    /// 档案ID（Product.Id，档案为主表后无独立库存ID）
    /// </summary>
    public long Id { get; set; }

    public long ProductId { get; set; }
    public decimal Quantity { get; set; }

    /// <summary>
    /// 低库存预警阈值（来源 Product.LowStockThreshold，未配置时为 null）
    /// </summary>
    public decimal? AlertQuantity { get; set; }

    /// <summary>
    /// 积压预警阈值（来源 Product.OverstockThreshold，未配置时为 null）
    /// </summary>
    public decimal? OverstockThreshold { get; set; }

    /// <summary>
    /// 库存状态（1:充足 2:偏低 3:不足 4:积压），按阈值计算
    /// </summary>
    public int InventoryStatus { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// 商品名称（联表 ProductMaster.Name，用于列表展示）
    /// </summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// 商品编码（联表 ProductMaster.Code，用于列表展示）
    /// </summary>
    public string ProductCode { get; set; } = string.Empty;

    /// <summary>
    /// 商品类型（1:实物商品 2:服务商品 3:耗材 4:样品 5:赠品）
    /// </summary>
    public int ProductType { get; set; }

    /// <summary>
    /// 商品分类名称（联表 ProductCategory.Name，用于列表展示）
    /// </summary>
    public string? CategoryName { get; set; }
}

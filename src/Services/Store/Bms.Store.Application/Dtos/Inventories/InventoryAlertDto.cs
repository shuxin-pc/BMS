namespace Bms.Store.Application.Dtos.Inventories;

/// <summary>
/// 库存预警输出 DTO
/// </summary>
public class InventoryAlertDto
{
    public long Id { get; set; }
    public long ProductId { get; set; }
    public int AlertType { get; set; }
    public decimal CurrentQuantity { get; set; }
    public decimal AlertValue { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public long? BatchId { get; set; }
    public bool IsProcessed { get; set; }
    public DateTime? ProcessedTime { get; set; }
    public string? ProcessedRemark { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    /// <summary>商品名称（联表 ProductMaster.Name）</summary>
    public string? ProductName { get; set; }

    /// <summary>商品编码（联表 ProductMaster.Code）</summary>
    public string? ProductCode { get; set; }

    /// <summary>商品分类名称（联表 ProductCategory.Name）</summary>
    public string? CategoryName { get; set; }

    /// <summary>批次号（联表 InventoryBatch.BatchNo，仅效期预警有值）</summary>
    public string? BatchNo { get; set; }

    /// <summary>门店名称（联表 Store.Name）</summary>
    public string? StoreName { get; set; }

    /// <summary>缺口数量（低库存预警时 = AlertValue - CurrentQuantity，其余类型为 0）</summary>
    public decimal ShortageAmount { get; set; }
}

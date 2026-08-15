namespace Bms.Store.Application.Dtos.Inventories;

/// <summary>
/// 库存流水输出 DTO
/// </summary>
public class InventoryLogDto
{
    public long Id { get; set; }
    public long ProductId { get; set; }
    public int Type { get; set; }
    public int? SourceType { get; set; }
    public long? SupplierId { get; set; }
    public decimal? UnitPrice { get; set; }
    public decimal Quantity { get; set; }
    public decimal BeforeQuantity { get; set; }
    public decimal AfterQuantity { get; set; }
    public string? BatchNo { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public long? RelatedId { get; set; }
    public string? Remark { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    /// <summary>商品名称（查询时 JOIN 填充）</summary>
    public string? ProductName { get; set; }

    /// <summary>商品编码（查询时 JOIN 填充）</summary>
    public string? ProductCode { get; set; }

    /// <summary>供应商名称（查询时 JOIN 填充）</summary>
    public string? SupplierName { get; set; }

    /// <summary>操作人ID（用于追责与同名操作人区分）</summary>
    public long? OperatorId { get; set; }

    /// <summary>操作人姓名（写入时冗余存储）</summary>
    public string? OperatorName { get; set; }

    /// <summary>操作前批次库存（按同商品同批次流水累加计算，无 productId 查询时为 null）</summary>
    public decimal? BatchBeforeQuantity { get; set; }

    /// <summary>操作后批次库存（按同商品同批次流水累加计算，无 productId 查询时为 null）</summary>
    public decimal? BatchAfterQuantity { get; set; }

    /// <summary>操作前商品总库存（按同商品流水累加计算，无 productId 查询时为 null）</summary>
    public decimal? TotalBeforeQuantity { get; set; }

    /// <summary>操作后商品总库存（按同商品流水累加计算，无 productId 查询时为 null）</summary>
    public decimal? TotalAfterQuantity { get; set; }
}

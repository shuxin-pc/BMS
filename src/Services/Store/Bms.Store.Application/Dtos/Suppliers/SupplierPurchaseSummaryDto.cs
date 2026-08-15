namespace Bms.Store.Application.Dtos.Suppliers;

/// <summary>
/// 供应商采购统计汇总
/// </summary>
public class SupplierPurchaseSummaryDto
{
    public long SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    /// <summary>去重采购单数（同一供应商多明细对应同一订单只算一次）</summary>
    public int OrderCount { get; set; }
    /// <summary>累计采购额（明细 TotalPrice 之和）</summary>
    public decimal TotalAmount { get; set; }
    /// <summary>去重商品数</summary>
    public int ProductCount { get; set; }
    /// <summary>最近采购时间（取 PurchaseOrder.OrderDate）</summary>
    public DateTime? LastPurchaseTime { get; set; }
    /// <summary>Top 5 商品明细（按采购额降序）</summary>
    public List<SupplierPurchaseSummaryProductDto> TopProducts { get; set; } = new();
}

/// <summary>
/// 供应商采购统计的商品明细
/// </summary>
public class SupplierPurchaseSummaryProductDto
{
    public long ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal TotalAmount { get; set; }
    /// <summary>该商品涉及的采购单数（去重）</summary>
    public int OrderCount { get; set; }
}

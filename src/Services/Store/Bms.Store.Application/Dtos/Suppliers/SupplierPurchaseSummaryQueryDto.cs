namespace Bms.Store.Application.Dtos.Suppliers;

/// <summary>
/// 供应商采购统计查询参数（复用采购订单的筛选条件）
/// </summary>
public class SupplierPurchaseSummaryQueryDto
{
    public long? SupplierId { get; set; }
    public long? ProductId { get; set; }
    public string? OrderNo { get; set; }
    public int? PurchaseType { get; set; }
    public DateTime? OrderDateStart { get; set; }
    public DateTime? OrderDateEnd { get; set; }
}

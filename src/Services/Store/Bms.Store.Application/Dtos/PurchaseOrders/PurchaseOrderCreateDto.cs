namespace Bms.Store.Application.Dtos.PurchaseOrders;

/// <summary>
/// 创建采购订单DTO
/// </summary>
public class PurchaseOrderCreateDto
{
    public string OrderNo { get; set; } = string.Empty;
    public long SupplierId { get; set; }
    public DateTime OrderDate { get; set; }
    public decimal TotalAmount { get; set; }
    public int Status { get; set; } = 1;
    public int PurchaseType { get; set; }
    public long? OperatorId { get; set; }
    public string? Remark { get; set; }

    /// <summary>
    /// 采购明细列表（级联创建，CreateAsync 中按明细联动库存）
    /// </summary>
    public List<PurchaseOrderItemCreateDto> Items { get; set; } = new();
}

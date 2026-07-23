namespace Bms.Store.Application.Dtos.PurchaseReturns;

/// <summary>
/// 采购退货输出 DTO
/// </summary>
public class PurchaseReturnDto
{
    public long Id { get; set; }
    public string ReturnNo { get; set; } = string.Empty;
    public long SupplierId { get; set; }

    /// <summary>
    /// 关联的原采购订单ID
    /// </summary>
    public long? PurchaseOrderId { get; set; }

    public decimal TotalQuantity { get; set; }
    public decimal TotalRefundAmount { get; set; }
    public DateTime ReturnTime { get; set; }
    public string? VoucherImageUrl { get; set; }
    public long? OperatorId { get; set; }
    public string? Remark { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// 退货明细列表
    /// </summary>
    public List<PurchaseReturnItemDto> Items { get; set; } = new();
}

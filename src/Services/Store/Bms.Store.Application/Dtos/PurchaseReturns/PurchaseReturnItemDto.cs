namespace Bms.Store.Application.Dtos.PurchaseReturns;

/// <summary>
/// 采购退货明细输出 DTO
/// </summary>
public class PurchaseReturnItemDto
{
    public long Id { get; set; }
    public long PurchaseReturnId { get; set; }
    public long ProductId { get; set; }
    public decimal Quantity { get; set; }
    public decimal RefundAmount { get; set; }
    public string? BatchNo { get; set; }
    public string? Remark { get; set; }
}

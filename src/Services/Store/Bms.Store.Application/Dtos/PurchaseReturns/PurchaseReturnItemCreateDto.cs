namespace Bms.Store.Application.Dtos.PurchaseReturns;

/// <summary>
/// 采购退货明细创建 DTO
/// </summary>
public class PurchaseReturnItemCreateDto
{
    /// <summary>商品ID</summary>
    public long ProductId { get; set; }

    /// <summary>退货数量</summary>
    public decimal Quantity { get; set; }

    /// <summary>退款金额</summary>
    public decimal RefundAmount { get; set; }

    /// <summary>批次号（可选）</summary>
    public string? BatchNo { get; set; }

    /// <summary>备注</summary>
    public string? Remark { get; set; }
}

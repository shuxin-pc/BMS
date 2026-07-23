namespace Bms.Store.Application.Dtos.PurchaseReturns;

/// <summary>
/// 创建采购退货输入 DTO
/// 支持一次退回多种商品，明细通过 Items 列表传入
/// </summary>
public class PurchaseReturnCreateDto
{
    public string ReturnNo { get; set; } = string.Empty;
    public long SupplierId { get; set; }

    /// <summary>
    /// 关联的原采购订单ID（可选，传入后退货时会冲减采购订单 RefundedAmount）
    /// </summary>
    public long? PurchaseOrderId { get; set; }

    public DateTime ReturnTime { get; set; }
    public string? VoucherImageUrl { get; set; }
    public long? OperatorId { get; set; }
    public string? Remark { get; set; }

    /// <summary>
    /// 退货明细列表
    /// </summary>
    public List<PurchaseReturnItemCreateDto> Items { get; set; } = new();
}

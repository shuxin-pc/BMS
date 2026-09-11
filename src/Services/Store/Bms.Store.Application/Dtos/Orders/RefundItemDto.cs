namespace Bms.Store.Application.Dtos.Orders;

/// <summary>
/// 退款退库明细项（门店在退款弹窗手动选择退回的批次 + 数量）
/// </summary>
public class RefundItemDto
{
    /// <summary>
    /// 订单明细批次ID（OrderItemBatch.Id，粒度 = 商品 × 批次）
    /// 服务项目/项目卡核销行的 BOM 耗材扣减明细同样是 OrderItemBatch（ProductId = 耗材），可直接选择退回
    /// </summary>
    public long OrderItemBatchId { get; set; }

    /// <summary>
    /// 退库数量（正数，≤ 可退数量 = Quantity - RefundedQuantity）
    /// </summary>
    public decimal Quantity { get; set; }
}

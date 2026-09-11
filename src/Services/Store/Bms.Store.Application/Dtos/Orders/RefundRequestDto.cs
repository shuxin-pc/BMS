namespace Bms.Store.Application.Dtos.Orders;

/// <summary>
/// 订单退款请求 DTO
/// </summary>
public class RefundRequestDto
{
    /// <summary>
    /// 订单ID
    /// </summary>
    public long OrderId { get; set; }

    /// <summary>
    /// 退款金额
    /// 规则：
    /// - OrderType=1/2 订单：必须 &gt; 0，且不超过实付金额减去已退款金额
    /// - OrderType=3 项目卡核销订单：必须 = 0（核销订单 PaidAmount=0，无实际款项退还，仅回退项目卡次数与库存/BOM）
    /// </summary>
    public decimal RefundAmount { get; set; }

    /// <summary>
    /// 退款原因
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// 退库明细列表（门店在退款弹窗手动选择退回的批次 + 数量，可空/为空表示本次退款不执行库存回退）
    /// 粒度：OrderItemBatch（商品 × 批次），只从订单中已有的批次选择退回，绝不新建退货批次；
    /// 服务项目/项目卡核销行的 BOM 耗材扣减明细（OrderItemBatch.ProductId = 耗材）同样在此选择退回。
    /// 退库数量与退款金额相互独立：金额按实付比例联动（储值/积分/统计），退库按门店勾选精确执行。
    /// </summary>
    public List<RefundItemDto>? RefundItems { get; set; }
}

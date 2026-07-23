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
    /// - OrderType=3 疗程卡核销订单：必须 = 0（核销订单 PaidAmount=0，无实际款项退还，仅回退疗程卡次数与库存/BOM）
    /// </summary>
    public decimal RefundAmount { get; set; }

    /// <summary>
    /// 退款原因
    /// </summary>
    public string Reason { get; set; } = string.Empty;
}

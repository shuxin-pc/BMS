namespace Bms.Store.Application.Dtos.Orders;

/// <summary>
/// 订单退款结果 DTO
/// </summary>
public class RefundResultDto
{
    /// <summary>
    /// 订单ID
    /// </summary>
    public long OrderId { get; set; }

    /// <summary>
    /// 订单编号
    /// </summary>
    public string OrderNo { get; set; } = string.Empty;

    /// <summary>
    /// 本次退款金额
    /// </summary>
    public decimal ThisRefundAmount { get; set; }

    /// <summary>
    /// 累计已退款金额
    /// </summary>
    public decimal TotalRefundedAmount { get; set; }

    /// <summary>
    /// 订单状态（2:已完成 3:已退款 4:已取消）
    /// </summary>
    public int OrderStatus { get; set; }

    /// <summary>
    /// 退款时间
    /// </summary>
    public DateTime RefundTime { get; set; }

    /// <summary>
    /// 联动操作记录（描述本次退款触发的库存、积分、项目卡、储值等联动操作）
    /// </summary>
    public List<string> Actions { get; set; } = new();

    /// <summary>
    /// 本次退款触发反日结的日期列表（跨日退款时，系统自动反日结原下单日，需店主重新确认）
    /// </summary>
    public List<DateTime> ReversedSettlementDates { get; set; } = new();
}

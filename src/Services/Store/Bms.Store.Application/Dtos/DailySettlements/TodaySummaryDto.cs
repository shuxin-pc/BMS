namespace Bms.Store.Application.Dtos.DailySettlements;

/// <summary>
/// 今日经营汇总数据（实时聚合，不落库）
/// </summary>
public class TodaySummaryDto
{
    /// <summary>
    /// 今日日期
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// 总营收（已完成订单实收金额合计）
    /// </summary>
    public decimal TotalRevenue { get; set; }

    /// <summary>
    /// 总退款（已退款订单退款金额合计）
    /// </summary>
    public decimal TotalRefund { get; set; }

    /// <summary>
    /// 储值充值总额
    /// </summary>
    public decimal TotalStoredValueRecharge { get; set; }

    /// <summary>
    /// 储值消费总额
    /// </summary>
    public decimal TotalStoredValueConsume { get; set; }

    /// <summary>
    /// 订单数（已完成 + 已退款）
    /// </summary>
    public int OrderCount { get; set; }

    /// <summary>
    /// 今日是否已存在日结记录（待确认或已确认）
    /// </summary>
    public bool IsSettled { get; set; }

    /// <summary>
    /// 已存在的日结记录ID（若有）
    /// </summary>
    public long? SettlementId { get; set; }

    /// <summary>
    /// 净营收 = 总营收 - 总退款（可为负）
    /// </summary>
    public decimal NetRevenue => TotalRevenue - TotalRefund;

    /// <summary>
    /// 退款是否大于营收（true 表示异常，前端需高亮提醒）
    /// </summary>
    public bool IsRefundExceedRevenue => TotalRefund > TotalRevenue;
}

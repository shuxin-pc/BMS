namespace Bms.Store.Application.Dtos.Dashboard;

/// <summary>
/// 首页看板今日核心指标
/// 实时聚合当日订单/储值/预约/预警数据
/// </summary>
public class DashboardSummaryDto
{
    /// <summary>
    /// 今日营收金额（已完成订单实付金额，= TodayCashRevenue + TodayStoredValueRevenue）
    /// </summary>
    public decimal TodayRevenue { get; set; }

    /// <summary>
    /// 今日现金类营收（现金+支付宝+微信+银行卡，含组合支付类别1部分）
    /// </summary>
    public decimal TodayCashRevenue { get; set; }

    /// <summary>
    /// 今日储值扣款营收（含组合支付类别2部分）
    /// </summary>
    public decimal TodayStoredValueRevenue { get; set; }

    /// <summary>
    /// 今日积分抵扣金额（仅记录，不纳入营收）
    /// </summary>
    public decimal TodayPointsDeductAmount { get; set; }

    /// <summary>
    /// 今日疗程卡核销折算金额（权责发生制转营收）
    /// </summary>
    public decimal TodayTreatmentCardVerifyAmount { get; set; }

    /// <summary>
    /// 今日总退款金额（现金退款 + 储值退款 + 积分退款）
    /// </summary>
    public decimal TodayRefundAmount { get; set; }

    /// <summary>
    /// 今日现金类退款金额（冲减营收）
    /// </summary>
    public decimal TodayCashRefundAmount { get; set; }

    /// <summary>
    /// 今日订单数（已完成+已退款）
    /// </summary>
    public int TodayOrderCount { get; set; }

    /// <summary>
    /// 今日毛利（营收-成本）
    /// </summary>
    public decimal TodayGrossProfit { get; set; }

    /// <summary>
    /// 今日消费客户数（去重）
    /// </summary>
    public int TodayConsumeCustomerCount { get; set; }

    /// <summary>
    /// 今日新客数
    /// </summary>
    public int TodayNewCustomerCount { get; set; }

    /// <summary>
    /// 今日预约数
    /// </summary>
    public int TodayAppointmentCount { get; set; }

    /// <summary>
    /// 库存预警数（未处理）
    /// </summary>
    public int InventoryAlertCount { get; set; }
}

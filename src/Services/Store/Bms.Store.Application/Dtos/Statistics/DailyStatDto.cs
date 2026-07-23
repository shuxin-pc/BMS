namespace Bms.Store.Application.Dtos.Statistics;

/// <summary>
/// 日统计输出 DTO
/// </summary>
public class DailyStatDto
{
    public long Id { get; set; }
    public DateTime StatDate { get; set; }
    public decimal Revenue { get; set; }
    public decimal CashRevenue { get; set; }
    public decimal StoredValueRevenue { get; set; }
    public decimal PointsDeductAmount { get; set; }
    public decimal Cost { get; set; }
    public decimal GrossProfit { get; set; }
    public int OrderCount { get; set; }
    public decimal RefundAmount { get; set; }
    /// <summary>
    /// 现金类退款金额（冲减营收）
    /// </summary>
    public decimal CashRefundAmount { get; set; }
    public decimal StoredValueRecharge { get; set; }
    public decimal StoredValueConsume { get; set; }
    public int ConsumeCustomerCount { get; set; }
    public int NewCustomerCount { get; set; }
    public int AppointmentCount { get; set; }
    public int InventoryAlertCount { get; set; }
    /// <summary>
    /// 疗程卡核销折算金额
    /// </summary>
    public decimal TreatmentCardVerifyAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

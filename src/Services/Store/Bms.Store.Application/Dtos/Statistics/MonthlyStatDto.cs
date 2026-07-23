namespace Bms.Store.Application.Dtos.Statistics;

/// <summary>
/// 月统计输出 DTO
/// </summary>
public class MonthlyStatDto
{
    public long Id { get; set; }
    public string StatMonth { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public decimal CashRevenue { get; set; }
    public decimal StoredValueRevenue { get; set; }
    public decimal PointsDeductAmount { get; set; }
    public decimal Cost { get; set; }
    public decimal GrossProfit { get; set; }
    public int OrderCount { get; set; }
    public decimal RefundAmount { get; set; }
    public decimal StoredValueRecharge { get; set; }
    public decimal StoredValueConsume { get; set; }
    public decimal TreatmentCardVerifyAmount { get; set; }
    public int ConsumeCustomerCount { get; set; }
    public int NewCustomerCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

namespace Bms.Store.Application.Dtos.Statistics;

/// <summary>
/// 创建月统计输入 DTO
/// </summary>
public class MonthlyStatCreateDto
{
    public string StatMonth { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public decimal Cost { get; set; }
    public decimal GrossProfit { get; set; }
    public int OrderCount { get; set; }
    public decimal RefundAmount { get; set; }
    public decimal StoredValueRecharge { get; set; }
    public decimal StoredValueConsume { get; set; }
}

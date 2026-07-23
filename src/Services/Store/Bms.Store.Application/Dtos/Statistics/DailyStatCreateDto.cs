namespace Bms.Store.Application.Dtos.Statistics;

/// <summary>
/// 创建日统计输入 DTO
/// </summary>
public class DailyStatCreateDto
{
    public DateTime StatDate { get; set; }
    public decimal Revenue { get; set; }
    public decimal Cost { get; set; }
    public decimal GrossProfit { get; set; }
    public int OrderCount { get; set; }
    public decimal RefundAmount { get; set; }
    public decimal StoredValueRecharge { get; set; }
    public decimal StoredValueConsume { get; set; }
    public int ConsumeCustomerCount { get; set; }
    public int NewCustomerCount { get; set; }
    public int AppointmentCount { get; set; }
    public int InventoryAlertCount { get; set; }
}

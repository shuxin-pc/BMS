namespace Bms.Store.Application.Dtos.Customers;

/// <summary>
/// 创建消费记录输入 DTO
/// </summary>
public class ConsumeLogCreateDto
{
    public long CustomerId { get; set; }
    public long OrderId { get; set; }
    public decimal Amount { get; set; }
    public int Points { get; set; }
    public DateTime ConsumeTime { get; set; }
    public string? Remark { get; set; }
}

namespace Bms.Store.Application.Dtos.Customers;

/// <summary>
/// 消费记录输出 DTO
/// </summary>
public class ConsumeLogDto
{
    public long Id { get; set; }
    public long CustomerId { get; set; }
    public long OrderId { get; set; }
    public decimal Amount { get; set; }
    public int Points { get; set; }
    public DateTime ConsumeTime { get; set; }
    public string? Remark { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

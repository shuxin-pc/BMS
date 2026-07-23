namespace Bms.Store.Application.Dtos.Customers;

/// <summary>
/// 客户积分流水输出 DTO
/// </summary>
public class CustomerPointsLogDto
{
    public long Id { get; set; }
    public long CustomerId { get; set; }
    public int Type { get; set; }
    public int Points { get; set; }
    public int BeforePoints { get; set; }
    public int AfterPoints { get; set; }
    public long? OrderId { get; set; }
    public long? OperatorId { get; set; }
    public DateTime? ExpireDate { get; set; }
    public string? Remark { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

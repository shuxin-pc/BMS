namespace Bms.Store.Application.Dtos.Customers;

/// <summary>
/// 创建客户积分流水输入 DTO
/// </summary>
public class CustomerPointsLogCreateDto
{
    public long CustomerId { get; set; }
    public int Type { get; set; }
    public int Points { get; set; }
    public int BeforePoints { get; set; }
    public int AfterPoints { get; set; }
    public long? OrderId { get; set; }
    public long? OperatorId { get; set; }
    public DateTime? ExpireDate { get; set; }
    public string? Remark { get; set; }
}

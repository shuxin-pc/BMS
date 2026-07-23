namespace Bms.Store.Application.Dtos.Customers;

/// <summary>
/// 客户档案输出 DTO
/// </summary>
public class CustomerDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public int Gender { get; set; }
    public DateTime? Birthday { get; set; }
    public long? LevelId { get; set; }
    public int TotalPoints { get; set; }
    public decimal Balance { get; set; }
    public decimal TotalConsume { get; set; }
    public DateTime? LastConsumeTime { get; set; }
    public string? Address { get; set; }
    public string? Tags { get; set; }
    public int AuthorizationStatus { get; set; }
    public DateTime? AuthorizationTime { get; set; }
    public string? Remark { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

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

    /// <summary>
    /// 客户等级名称（来自导航属性 Level.Name，便于前端直接展示）
    /// </summary>
    public string? LevelName { get; set; }

    public int TotalPoints { get; set; }
    public decimal Balance { get; set; }
    public decimal TotalConsume { get; set; }
    public DateTime? LastConsumeTime { get; set; }
    public string? Address { get; set; }
    public List<CustomerTagBriefDto> Tags { get; set; } = new();
    public int AuthorizationStatus { get; set; }
    public DateTime? AuthorizationTime { get; set; }
    public string? Remark { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

namespace Bms.Store.Application.Dtos.Customers;

/// <summary>
/// 创建客户档案输入 DTO
/// </summary>
public class CustomerCreateDto
{
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public int Gender { get; set; }
    public DateTime? Birthday { get; set; }
    public long? LevelId { get; set; }
    public string? Address { get; set; }
    public string? Tags { get; set; }
    public int AuthorizationStatus { get; set; }
    public DateTime? AuthorizationTime { get; set; }
    public string? Remark { get; set; }
}

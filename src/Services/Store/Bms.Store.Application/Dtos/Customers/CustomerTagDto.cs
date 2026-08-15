namespace Bms.Store.Application.Dtos.Customers;

/// <summary>
/// 客户标签输出 DTO
/// </summary>
public class CustomerTagDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Color { get; set; }
    public int Sort { get; set; }
    public string? Remark { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

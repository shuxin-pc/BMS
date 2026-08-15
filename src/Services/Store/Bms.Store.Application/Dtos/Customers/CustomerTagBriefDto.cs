namespace Bms.Store.Application.Dtos.Customers;

/// <summary>
/// 客户标签简要信息（嵌套在 CustomerDto 中返回）
/// </summary>
public class CustomerTagBriefDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Color { get; set; }
}

namespace Bms.Store.Application.Dtos.Customers;

/// <summary>
/// 更新客户标签输入 DTO
/// </summary>
public class CustomerTagUpdateDto : CustomerTagCreateDto
{
    public long Id { get; set; }
}

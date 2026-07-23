namespace Bms.Store.Application.Dtos.Customers;

/// <summary>
/// 更新客户档案输入 DTO
/// </summary>
public class CustomerUpdateDto : CustomerCreateDto
{
    public long Id { get; set; }
}

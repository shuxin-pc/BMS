namespace Bms.Store.Application.Dtos.Customers;

/// <summary>
/// 更新客户美容档案输入 DTO
/// </summary>
public class CustomerBeautyProfileUpdateDto : CustomerBeautyProfileCreateDto
{
    public long Id { get; set; }
}

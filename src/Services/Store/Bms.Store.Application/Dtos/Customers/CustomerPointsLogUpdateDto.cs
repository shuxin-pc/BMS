namespace Bms.Store.Application.Dtos.Customers;

/// <summary>
/// 更新客户积分流水输入 DTO
/// </summary>
public class CustomerPointsLogUpdateDto : CustomerPointsLogCreateDto
{
    public long Id { get; set; }
}

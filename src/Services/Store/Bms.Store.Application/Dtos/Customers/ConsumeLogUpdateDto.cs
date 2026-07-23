namespace Bms.Store.Application.Dtos.Customers;

/// <summary>
/// 更新消费记录输入 DTO
/// </summary>
public class ConsumeLogUpdateDto : ConsumeLogCreateDto
{
    public long Id { get; set; }
}

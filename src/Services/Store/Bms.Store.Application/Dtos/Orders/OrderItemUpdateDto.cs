namespace Bms.Store.Application.Dtos.Orders;

/// <summary>
/// 更新订单明细输入 DTO
/// </summary>
public class OrderItemUpdateDto : OrderItemCreateDto
{
    public long Id { get; set; }
}

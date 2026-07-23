using Bms.Store.Application.Dtos;

namespace Bms.Store.Application.Dtos.Orders;

/// <summary>
/// 订单明细分页查询参数
/// </summary>
public class OrderItemQueryDto : PagedRequestDto
{
    /// <summary>
    /// 订单ID
    /// </summary>
    public long? OrderId { get; set; }
}

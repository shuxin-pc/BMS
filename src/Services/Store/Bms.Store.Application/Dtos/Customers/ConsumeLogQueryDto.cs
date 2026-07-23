using Bms.Store.Application.Dtos;

namespace Bms.Store.Application.Dtos.Customers;

/// <summary>
/// 消费记录分页查询参数
/// </summary>
public class ConsumeLogQueryDto : PagedRequestDto
{
    /// <summary>
    /// 客户ID
    /// </summary>
    public long? CustomerId { get; set; }

    /// <summary>
    /// 订单ID
    /// </summary>
    public long? OrderId { get; set; }
}

using Bms.Store.Application.Dtos;

namespace Bms.Store.Application.Dtos.Customers;

/// <summary>
/// 客户积分流水分页查询参数
/// </summary>
public class CustomerPointsLogQueryDto : PagedRequestDto
{
    /// <summary>
    /// 客户ID
    /// </summary>
    public long? CustomerId { get; set; }

    /// <summary>
    /// 积分类型（1:消费获得 2:退款扣减 3:积分兑换 4:调整）
    /// </summary>
    public int? Type { get; set; }
}

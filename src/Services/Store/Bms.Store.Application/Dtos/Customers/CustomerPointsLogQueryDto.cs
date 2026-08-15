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
    /// 积分类型（参见 CustomerPointsLogType：1:消费获得 2:积分抵扣 3:退款扣减 5:充值获得 6:疗程卡购买获得 7:过期清零 8:手动调整）
    /// </summary>
    public int? Type { get; set; }

    /// <summary>
    /// 客户姓名（模糊匹配）
    /// </summary>
    public string? CustomerName { get; set; }

    /// <summary>
    /// 手机号（模糊匹配）
    /// </summary>
    public string? Phone { get; set; }
}

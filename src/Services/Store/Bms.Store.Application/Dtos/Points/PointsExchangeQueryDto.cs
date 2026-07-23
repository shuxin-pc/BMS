using Bms.Store.Application.Dtos;

namespace Bms.Store.Application.Dtos.Points;

/// <summary>
/// 积分兑换记录分页查询 DTO
/// </summary>
public class PointsExchangeQueryDto : PagedRequestDto
{
    /// <summary>
    /// 客户ID
    /// </summary>
    public long? CustomerId { get; set; }

    /// <summary>
    /// 兑换类型（1:商品 2:优惠券）
    /// </summary>
    public int? ExchangeType { get; set; }
}

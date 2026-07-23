using Bms.Store.Application.Dtos;

namespace Bms.Store.Application.Dtos.StoredValues;

/// <summary>
/// 储值流水分页查询 DTO
/// </summary>
public class StoredValueLogQueryDto : PagedRequestDto
{
    /// <summary>
    /// 客户ID
    /// </summary>
    public long? CustomerId { get; set; }

    /// <summary>
    /// 流水类型（1:充值 2:消费 3:退款 4:调整）
    /// </summary>
    public int? Type { get; set; }
}

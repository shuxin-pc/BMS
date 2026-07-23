using Bms.Store.Application.Dtos;

namespace Bms.Store.Application.Dtos.PriceChangeLogs;

/// <summary>
/// 价格变更记录分页查询参数
/// </summary>
public class PriceChangeLogQueryDto : PagedRequestDto
{
    /// <summary>
    /// 商品ID
    /// </summary>
    public long? ProductId { get; set; }
}

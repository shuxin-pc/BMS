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

    /// <summary>
    /// 商品名称（模糊匹配，基于 Product.Master.Name）
    /// </summary>
    public string? ProductName { get; set; }

    /// <summary>
    /// 变更开始日期（ChangeTime >= StartDate）
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// 变更结束日期（ChangeTime <= EndDate，含当日）
    /// </summary>
    public DateTime? EndDate { get; set; }
}

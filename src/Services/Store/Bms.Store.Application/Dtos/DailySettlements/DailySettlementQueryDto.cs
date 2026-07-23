using Bms.Store.Application.Dtos;

namespace Bms.Store.Application.Dtos.DailySettlements;

/// <summary>
/// 日结记录分页查询 DTO
/// </summary>
public class DailySettlementQueryDto : PagedRequestDto
{
    /// <summary>
    /// 日结日期起始（含）
    /// </summary>
    public DateTime? SettlementDateStart { get; set; }

    /// <summary>
    /// 日结日期结束（含）
    /// </summary>
    public DateTime? SettlementDateEnd { get; set; }

    /// <summary>
    /// 状态筛选（0:待确认 1:已确认）
    /// </summary>
    public int? Status { get; set; }

    /// <summary>
    /// 门店ID筛选（可选，默认取当前用户门店）
    /// </summary>
    public long? StoreId { get; set; }
}

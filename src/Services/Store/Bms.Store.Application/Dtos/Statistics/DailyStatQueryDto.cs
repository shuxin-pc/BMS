using Bms.Store.Application.Dtos;

namespace Bms.Store.Application.Dtos.Statistics;

/// <summary>
/// 日统计分页查询参数
/// </summary>
public class DailyStatQueryDto : PagedRequestDto
{
    /// <summary>
    /// 统计开始日期
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// 统计结束日期
    /// </summary>
    public DateTime? EndDate { get; set; }
}

using Bms.Store.Application.Dtos;

namespace Bms.Store.Application.Dtos.Statistics;

/// <summary>
/// 月统计分页查询参数
/// </summary>
public class MonthlyStatQueryDto : PagedRequestDto
{
    /// <summary>
    /// 统计月份（yyyy-MM）
    /// </summary>
    public string? StatMonth { get; set; }
}

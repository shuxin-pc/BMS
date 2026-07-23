namespace Bms.Store.Application.Dtos.Dashboard;

/// <summary>
/// 月度营收趋势查询参数
/// </summary>
public class MonthlyTrendQueryDto
{
    /// <summary>
    /// 年份
    /// </summary>
    public int Year { get; set; }

    /// <summary>
    /// 月份（1-12）
    /// </summary>
    public int Month { get; set; }
}

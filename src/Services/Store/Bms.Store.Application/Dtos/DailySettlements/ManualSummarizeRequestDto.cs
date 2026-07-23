namespace Bms.Store.Application.Dtos.DailySettlements;

/// <summary>
/// 手动汇总日结请求
/// </summary>
public class ManualSummarizeRequestDto
{
    /// <summary>
    /// 汇总日期（默认今日）
    /// </summary>
    public DateTime? Date { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}

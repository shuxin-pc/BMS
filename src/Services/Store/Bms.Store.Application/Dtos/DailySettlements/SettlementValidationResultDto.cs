namespace Bms.Store.Application.Dtos.DailySettlements;

/// <summary>
/// 日结确认前防漏单校验结果
/// </summary>
public class SettlementValidationResultDto
{
    /// <summary>
    /// 是否可以确认（重复日结会阻止确认）
    /// </summary>
    public bool CanConfirm { get; set; }

    /// <summary>
    /// 警告信息列表（前置连续性、异常数据等提醒，不阻止确认）
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// 缺失日结的具体日期列表（最多返回 30 天，便于前端日历视图展示）
    /// </summary>
    public List<DateTime> MissingDates { get; set; } = new();
}

namespace Bms.Store.Application.Dtos.DailySettlements;

/// <summary>
/// 确认日结请求
/// </summary>
public class ConfirmRequestDto
{
    /// <summary>
    /// 日结备注（确认时可修改）
    /// null 表示不修改保持原值，空字符串表示清空备注
    /// </summary>
    public string? Remark { get; set; }
}

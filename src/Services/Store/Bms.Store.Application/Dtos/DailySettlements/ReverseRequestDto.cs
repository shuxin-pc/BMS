namespace Bms.Store.Application.Dtos.DailySettlements;

/// <summary>
/// 反日结请求
/// </summary>
public class ReverseRequestDto
{
    /// <summary>
    /// 反日结原因（可选，建议填写但不强制）
    /// </summary>
    public string? Reason { get; set; }
}

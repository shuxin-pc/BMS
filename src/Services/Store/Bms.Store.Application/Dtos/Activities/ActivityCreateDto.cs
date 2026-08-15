namespace Bms.Store.Application.Dtos.Activities;

/// <summary>
/// 创建活动请求 DTO
/// </summary>
public class ActivityCreateDto
{
    /// <summary>
    /// 活动名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 开始时间
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// 结束时间（不能早于开始时间）
    /// </summary>
    public DateTime EndTime { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}

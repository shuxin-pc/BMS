namespace Bms.Store.Application.Dtos.Activities;

/// <summary>
/// 活动下拉选项 DTO（仅返回进行中活动，供其他业务选择关联）
/// </summary>
public class ActivityOptionDto
{
    /// <summary>活动ID</summary>
    public long Id { get; set; }

    /// <summary>活动名称</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>开始时间</summary>
    public DateTime StartTime { get; set; }

    /// <summary>结束时间</summary>
    public DateTime EndTime { get; set; }
}

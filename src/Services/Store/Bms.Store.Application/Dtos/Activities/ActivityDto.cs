namespace Bms.Store.Application.Dtos.Activities;

/// <summary>
/// 活动 DTO
/// </summary>
public class ActivityDto
{
    public long Id { get; set; }

    /// <summary>
    /// 活动名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 开始时间
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// 结束时间
    /// </summary>
    public DateTime EndTime { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}

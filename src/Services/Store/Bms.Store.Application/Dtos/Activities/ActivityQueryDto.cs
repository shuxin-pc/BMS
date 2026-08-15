using Bms.Store.Application.Dtos;

namespace Bms.Store.Application.Dtos.Activities;

/// <summary>
/// 活动分页查询 DTO
/// </summary>
public class ActivityQueryDto : PagedRequestDto
{
    /// <summary>
    /// 活动名称（模糊查询）
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// 活动时间区间起始日（活动周期与该区间有重叠即命中）
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// 活动时间区间截止日（含当天，活动周期与该区间有重叠即命中）
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// 活动状态筛选：notStarted（未开始）/ ongoing（进行中）/ ended（已结束），为空则不过滤
    /// </summary>
    public string? Status { get; set; }
}

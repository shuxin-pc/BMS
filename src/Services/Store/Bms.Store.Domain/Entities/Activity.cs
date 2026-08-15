namespace Bms.Store.Domain.Entities;

/// <summary>
/// 活动台账（营销活动基础信息，供样品/赠品领用、订单内赠品关联归因）
/// </summary>
public class Activity : StoreEntity
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
    /// 结束时间（不能早于 StartTime）
    /// </summary>
    public DateTime EndTime { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}

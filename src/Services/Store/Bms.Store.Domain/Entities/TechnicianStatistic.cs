namespace Bms.Store.Domain.Entities;

/// <summary>
/// 技师统计
/// </summary>
public class TechnicianStatistic : StoreBusinessEntityBase
{
    /// <summary>
    /// 技师ID
    /// </summary>
    public long TechnicianId { get; set; }

    /// <summary>
    /// 统计日期
    /// </summary>
    public DateTime StatDate { get; set; }

    /// <summary>
    /// 服务人次
    /// </summary>
    public int ServiceCount { get; set; }

    /// <summary>
    /// 服务总时长（分钟）
    /// </summary>
    public int ServiceMinutes { get; set; }

    /// <summary>
    /// 技师服务费用汇总（OrderItem.TechnicianFee 累加）
    /// </summary>
    public decimal TotalTechnicianFee { get; set; }

    /// <summary>
    /// 回头客数量
    /// </summary>
    public int ReturnCustomerCount { get; set; }

    /// <summary>
    /// 导航属性：技师
    /// </summary>
    public Technician? Technician { get; set; }
}

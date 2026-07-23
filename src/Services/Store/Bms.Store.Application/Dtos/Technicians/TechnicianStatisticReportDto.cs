namespace Bms.Store.Application.Dtos.Technicians;

/// <summary>
/// 技师业绩统计报表 DTO（从 OrderItem 实时聚合，仅商家技师）
/// 对应需求 B4.5：服务人次、时长、回头客率、技师服务费用汇总
/// </summary>
public class TechnicianStatisticReportDto
{
    /// <summary>
    /// 技师ID
    /// </summary>
    public long TechnicianId { get; set; }

    /// <summary>
    /// 技师姓名
    /// </summary>
    public string TechnicianName { get; set; } = string.Empty;

    /// <summary>
    /// 服务人次
    /// </summary>
    public int ServiceCount { get; set; }

    /// <summary>
    /// 服务总时长（分钟）
    /// </summary>
    public int ServiceMinutes { get; set; }

    /// <summary>
    /// 技师服务费用汇总
    /// </summary>
    public decimal TotalTechnicianFee { get; set; }

    /// <summary>
    /// 总客户数（去重）
    /// </summary>
    public int TotalCustomerCount { get; set; }

    /// <summary>
    /// 回头客数量（统计期间内被同一技师服务 >1 次的客户数）
    /// </summary>
    public int ReturnCustomerCount { get; set; }
}

using Bms.Store.Application.Dtos;

namespace Bms.Store.Application.Dtos.Technicians;

/// <summary>
/// 技师统计分页查询参数
/// </summary>
public class TechnicianStatisticQueryDto : PagedRequestDto
{
    /// <summary>
    /// 技师ID
    /// </summary>
    public long? TechnicianId { get; set; }

    /// <summary>
    /// 统计开始日期
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// 统计结束日期
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// 是否强制查看报表（仅管理员可设置，用于运维排查纯平台技师门店）
    /// 默认 false：纯平台技师门店返回隐藏标识；true：管理员强制返回报表数据
    /// </summary>
    public bool Force { get; set; }
}

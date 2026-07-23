using Bms.Store.Application.Dtos;

namespace Bms.Store.Application.Dtos.Appointments;

/// <summary>
/// 预约分页查询参数
/// </summary>
public class AppointmentQueryDto : PagedRequestDto
{
    /// <summary>
    /// 客户ID
    /// </summary>
    public long? CustomerId { get; set; }

    /// <summary>
    /// 预约状态（1:待确认 2:已预约 3:已到店 4:已完成 5:已取消 6:爽约）
    /// </summary>
    public int? Status { get; set; }

    /// <summary>
    /// 预约日期起始
    /// </summary>
    public DateTime? AppointmentDateStart { get; set; }

    /// <summary>
    /// 预约日期截止
    /// </summary>
    public DateTime? AppointmentDateEnd { get; set; }

    /// <summary>
    /// 技师来源筛选（1:平台技师 2:商家技师）
    /// </summary>
    public int? TechnicianSource { get; set; }
}

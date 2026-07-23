namespace Bms.Store.Application.Dtos.Appointments;

/// <summary>
/// 预约明日提醒信息
/// </summary>
public class TomorrowReminderDto
{
    /// <summary>
    /// 预约ID
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 预约编号
    /// </summary>
    public string AppointmentNo { get; set; } = string.Empty;

    /// <summary>
    /// 客户名称
    /// </summary>
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>
    /// 手机号
    /// </summary>
    public string Phone { get; set; } = string.Empty;

    /// <summary>
    /// 服务项目
    /// </summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    /// 技师名称（关联 Technician 表，未分配时为空）
    /// </summary>
    public string? TechnicianName { get; set; }

    /// <summary>
    /// 预约时间（yyyy-MM-dd HH:mm 格式字符串）
    /// </summary>
    public string AppointmentTime { get; set; } = string.Empty;

    /// <summary>
    /// 提醒状态（1:待提醒 2:已提醒）
    /// </summary>
    public int RemindStatus { get; set; }

    /// <summary>
    /// 客户确认状态（1:待确认 2:已确认 3:需改期）
    /// 由预约状态映射：1->1, 2->2, 5->3
    /// </summary>
    public int CustomerConfirmStatus { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}

using Bms.Store.Domain.Entities;

namespace Bms.Store.Application.Dtos.Appointments;

/// <summary>
/// 创建预约输入 DTO
/// 预约号由后端 AppointmentNoGenerator 自动生成（AP{yyyyMMdd}{序号}），前端无需传入
/// </summary>
public class AppointmentCreateDto
{
    public long CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    /// <summary>
    /// 预约开始时间（一体格式，含日期与时刻，如 2026-08-25T22:00:00）
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// 服务项目商品ID（关联 Product 主表，type=2 服务项目）
    /// 后端根据 ProductId 查 ServiceProduct.Duration 自动计算 EndTime
    /// </summary>
    public long ProductId { get; set; }

    /// <summary>
    /// 预约状态（默认已预约，创建即生效，无"待确认"中间态）
    /// </summary>
    public int Status { get; set; } = AppointmentStatus.Confirmed;
    public long? TechnicianId { get; set; }
    public long? RoomId { get; set; }
    public long? EquipmentId { get; set; }
    public string? Remark { get; set; }
    public DateTime? ConfirmTime { get; set; }
    public DateTime? ArrivalTime { get; set; }
    public DateTime? CompleteTime { get; set; }
}

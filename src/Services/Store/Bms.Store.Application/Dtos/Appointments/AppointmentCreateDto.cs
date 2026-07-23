namespace Bms.Store.Application.Dtos.Appointments;

/// <summary>
/// 创建预约输入 DTO
/// </summary>
public class AppointmentCreateDto
{
    public string AppointmentNo { get; set; } = string.Empty;
    public long CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public DateTime AppointmentDate { get; set; }
    public TimeSpan AppointmentTime { get; set; }

    /// <summary>
    /// 服务项目商品ID（关联 Product 主表，type=2 服务项目）
    /// 后端根据 ProductId 查 ServiceProduct.Duration 自动计算 EndTime
    /// </summary>
    public long ProductId { get; set; }

    public int Status { get; set; } = 1;
    public long? TechnicianId { get; set; }
    public long? RoomId { get; set; }
    public long? EquipmentId { get; set; }
    public string? Remark { get; set; }
    public DateTime? ConfirmTime { get; set; }
    public DateTime? ArrivalTime { get; set; }
    public DateTime? CompleteTime { get; set; }
}

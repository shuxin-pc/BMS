namespace Bms.Store.Application.Dtos.Appointments;

/// <summary>
/// 预约输出 DTO
/// </summary>
public class AppointmentDto
{
    public long Id { get; set; }
    public string AppointmentNo { get; set; } = string.Empty;
    public long CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    /// <summary>
    /// 预约开始时间（一体格式，含日期与时刻；服务跨日结束时 EndTime 会落在次日）
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// 预计结束时间（由后端根据 ServiceProduct.Duration 自动计算，支持跨日）
    /// </summary>
    public DateTime? EndTime { get; set; }

    public int Status { get; set; }
    public long? TechnicianId { get; set; }

    /// <summary>
    /// 技师名称（关联 Technician 表填充，用于列表/详情展示）
    /// </summary>
    public string? TechnicianName { get; set; }

    /// <summary>
    /// 技师来源（1=商家技师 2=平台技师），通过关联 Technician 表填充
    /// </summary>
    public int? TechnicianSource { get; set; }

    public long? RoomId { get; set; }

    /// <summary>
    /// 房间/床位名称（关联 Room 表填充，用于日历/列表/详情展示）
    /// </summary>
    public string? RoomName { get; set; }

    public long? EquipmentId { get; set; }

    /// <summary>
    /// 设备名称（关联 Equipment 表填充，用于列表/详情展示）
    /// </summary>
    public string? EquipmentName { get; set; }

    /// <summary>
    /// 服务项目商品ID（关联 Product 主表，type=2 服务项目）
    /// </summary>
    public long ProductId { get; set; }

    /// <summary>
    /// 服务项目商品名称（由后端 Join Product 表填充，替代原 ServiceItem 字符串字段）
    /// </summary>
    public string? ProductName { get; set; }

    public string? Remark { get; set; }
    public DateTime? ConfirmTime { get; set; }
    public DateTime? ArrivalTime { get; set; }
    public DateTime? CompleteTime { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

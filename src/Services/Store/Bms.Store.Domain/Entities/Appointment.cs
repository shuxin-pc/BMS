namespace Bms.Store.Domain.Entities;

/// <summary>
/// 预约
/// </summary>
public class Appointment : StoreBusinessEntityBase
{
    /// <summary>
    /// 预约编号
    /// </summary>
    public string AppointmentNo { get; set; } = string.Empty;

    /// <summary>
    /// 客户ID
    /// </summary>
    public long CustomerId { get; set; }

    /// <summary>
    /// 客户姓名（冗余存储）
    /// </summary>
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>
    /// 客户电话（冗余存储）
    /// </summary>
    public string CustomerPhone { get; set; } = string.Empty;

    /// <summary>
    /// 预约开始时间（一体格式，含日期与时刻；服务跨日结束时 EndTime 会落在次日）
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// 预计结束时间（用于防冲突判断，由后端根据 ProductId 关联的 ServiceProduct.Duration 自动计算，支持跨日）
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// 预约状态（1:已预约 2:已到店 3:已完成 4:已取消 5:爽约）
    /// 创建即已预约;已取消:门店人员手动点击取消;爽约:超过预约时段未到店,系统定时任务自动更改
    /// </summary>
    public int Status { get; set; } = AppointmentStatus.Confirmed;

    /// <summary>
    /// 技师ID
    /// </summary>
    public long? TechnicianId { get; set; }

    /// <summary>
    /// 技师来源（1=自有技师，2=平台技师），由 AppointmentAppService.CreateAsync 根据 Technician.Source 自动填充
    /// 预约转订单时复制到 OrderItem.TechnicianSource，用于报表按技师来源分组统计
    /// </summary>
    public int? TechnicianSource { get; set; }

    /// <summary>
    /// 房间/床位ID（关联 Room，可空表示未分配）
    /// </summary>
    public long? RoomId { get; set; }

    /// <summary>
    /// 设备ID（关联 Equipment，可空表示未指定设备；某些服务项目需要特定设备）
    /// </summary>
    public long? EquipmentId { get; set; }

    /// <summary>
    /// 服务项目商品ID（关联 Product 主表，type=2 服务项目）
    /// 服务时长通过 ProductId -> ServiceProduct.Duration 获取，不在预约表冗余存储
    /// </summary>
    public long ProductId { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 确认时间
    /// </summary>
    public DateTime? ConfirmTime { get; set; }

    /// <summary>
    /// 到店时间
    /// </summary>
    public DateTime? ArrivalTime { get; set; }

    /// <summary>
    /// 完成时间
    /// </summary>
    public DateTime? CompleteTime { get; set; }

    /// <summary>
    /// 提醒状态（1:待提醒 2:已提醒）
    /// </summary>
    public int ReminderStatus { get; set; } = 1;

    /// <summary>
    /// 提醒发送时间
    /// </summary>
    public DateTime? ReminderTime { get; set; }

    /// <summary>
    /// 导航属性：客户
    /// </summary>
    public Customer? Customer { get; set; }

    /// <summary>
    /// 导航属性：设备
    /// </summary>
    public Equipment? Equipment { get; set; }

    /// <summary>
    /// 导航属性：服务项目商品（type=2 服务项目）
    /// </summary>
    public Product? Product { get; set; }
}

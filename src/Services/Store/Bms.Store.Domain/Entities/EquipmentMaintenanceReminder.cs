namespace Bms.Store.Domain.Entities;

/// <summary>
/// 设备保养提醒记录
/// 由后台任务 EquipmentMaintenanceReminderService 每日扫描设备 NextMaintenanceDate 自动生成，
/// 在保养记录创建后由 EquipmentMaintenanceAppService.CreateAsync 关闭（IsHandled=true）。
/// 提醒记录为流水性质，不进行软删除；同一设备同一日期仅生成一条，避免重复打扰。
/// </summary>
public class EquipmentMaintenanceReminder : StoreBusinessEntityBase
{
    /// <summary>
    /// 关联设备 ID
    /// </summary>
    public long EquipmentId { get; set; }

    /// <summary>
    /// 提醒生成日期（扫描日，用于同设备同日去重）
    /// </summary>
    public DateTime ReminderDate { get; set; }

    /// <summary>
    /// 目标保养日期（取自生成提醒时的设备 NextMaintenanceDate）
    /// </summary>
    public DateTime TargetMaintenanceDate { get; set; }

    /// <summary>
    /// 提醒类型：1=即将到期（目标保养日未过但临近） 2=已过期未保养（目标保养日已过）
    /// </summary>
    public int ReminderType { get; set; }

    /// <summary>
    /// 是否已处理（保养记录创建后置 true，避免重复提醒）
    /// </summary>
    public bool IsHandled { get; set; }

    /// <summary>
    /// 处理时间（关闭提醒时写入）
    /// </summary>
    public DateTime? HandledTime { get; set; }

    /// <summary>
    /// 导航属性：设备
    /// </summary>
    public Equipment? Equipment { get; set; }
}

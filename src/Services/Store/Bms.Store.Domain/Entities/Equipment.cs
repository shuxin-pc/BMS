namespace Bms.Store.Domain.Entities;

/// <summary>
/// 仪器设备台账（设备实例）
/// 一台具体设备归属于一个 EquipmentType；与服务项目通过类型关联，预约时再绑定具体实例
/// </summary>
public class Equipment : StoreEntity
{
    /// <summary>
    /// 所属设备类型 ID
    /// </summary>
    public long EquipmentTypeId { get; set; }

    /// <summary>
    /// 设备实例标识（如"1号机"、"A区3号"），用于门店人员识别
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 设备实例编号，门店范围内唯一
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 型号
    /// </summary>
    public string? Model { get; set; }

    /// <summary>
    /// 厂商
    /// </summary>
    public string? Manufacturer { get; set; }

    /// <summary>
    /// 购买日期
    /// </summary>
    public DateTime? PurchaseDate { get; set; }

    /// <summary>
    /// 购买价格
    /// </summary>
    public decimal? PurchasePrice { get; set; }

    /// <summary>
    /// 状态：1=正常 2=维修中 3=已停用（参见 <see cref="Bms.Store.Domain.Constants.EquipmentStatus"/>）
    /// </summary>
    public int Status { get; set; } = 1;

    /// <summary>
    /// 位置
    /// </summary>
    public string? Location { get; set; }

    /// <summary>
    /// 上次保养日期
    /// </summary>
    public DateTime? LastMaintenanceDate { get; set; }

    /// <summary>
    /// 下次保养日期（用于保养提醒）
    /// </summary>
    public DateTime? NextMaintenanceDate { get; set; }

    /// <summary>
    /// 保养周期（天）。null=不定期/手动指定下次保养日期；非空时可用于保养记录创建后自动推算下次保养日
    /// </summary>
    public int? MaintenanceCycleDays { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 导航属性：所属设备类型
    /// </summary>
    public EquipmentType? EquipmentType { get; set; }
}

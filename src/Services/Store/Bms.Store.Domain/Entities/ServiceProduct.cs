namespace Bms.Store.Domain.Entities;

/// <summary>
/// 服务商品子表（租户级，关联 ProductMaster）
/// 对应设计文档 3.4 节
/// 服务时长、所需房型、适用技能、设备类型属于商品本质属性，归 Master 层
/// </summary>
public class ServiceProduct : StoreTenantEntity
{
    /// <summary>
    /// 关联商品主档ID（引用 ProductMaster.Id）
    /// </summary>
    public long MasterId { get; set; }

    /// <summary>
    /// 服务时长（分钟）
    /// </summary>
    public int? Duration { get; set; }

    /// <summary>
    /// 所需房间/床位类型（1:房间 2:床位，null=不限）
    /// 用于预约资源匹配
    /// </summary>
    public int? RequiredRoomType { get; set; }

    /// <summary>
    /// 所需设备类型列表（结构化关联，替代旧的 RequiredEquipment 字符串字段）
    /// </summary>
    public List<ServiceProductEquipment> RequiredEquipmentTypes { get; set; } = new();

    /// <summary>
    /// 适用技师技能标签（历史冗余字符串字段，已废弃；结构化技能见 ServiceProductSkills）
    /// </summary>
    public string? ApplicableSkills { get; set; }

    /// <summary>
    /// 导航属性：商品主档
    /// </summary>
    public ProductMaster? Master { get; set; }

    /// <summary>
    /// 导航属性：适用技能标签关联（按门店语境配置）
    /// </summary>
    public List<ServiceProductSkill> ServiceProductSkills { get; set; } = new();
}

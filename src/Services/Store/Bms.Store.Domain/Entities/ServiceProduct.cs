namespace Bms.Store.Domain.Entities;

/// <summary>
/// 服务商品子表
/// 对应设计决策：商品多态模型（主表+子表）
/// 服务商品特有字段：服务时长、所需耗材、适用技师技能
/// </summary>
public class ServiceProduct : StoreEntity
{
    /// <summary>
    /// 关联商品ID
    /// </summary>
    public long ProductId { get; set; }

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
    /// 适用技师技能标签（JSON格式或逗号分隔）
    /// </summary>
    public string? ApplicableSkills { get; set; }

    /// <summary>
    /// 导航属性：商品
    /// </summary>
    public Product? Product { get; set; }
}

namespace Bms.Store.Application.Dtos.Products;

/// <summary>
/// 商品主档输出 DTO（租户级，承载商品本质属性）
/// 对应设计文档 4.1 节 Master 字段
/// </summary>
public class ProductMasterDto
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Type { get; set; }
    public long CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public string? Unit { get; set; }
    public string? Specification { get; set; }
    public string? Brand { get; set; }
    public string? ImageUrl { get; set; }
    /// <summary>是否可销售（样品/赠品为 false，跟 Type 走）</summary>
    public bool IsSalable { get; set; }
    public string? Remark { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // ========== 服务项目子表字段（Type=2）==========
    /// <summary>服务时长（分钟，服务项目）</summary>
    public int? Duration { get; set; }
    /// <summary>所需房间/床位类型（1:房间 2:床位，null=不限，服务项目）</summary>
    public int? RequiredRoomType { get; set; }
    /// <summary>所需设备类型 ID 列表（服务项目）</summary>
    public List<long> EquipmentTypeIds { get; set; } = new();
    /// <summary>所需设备类型名称列表（服务项目，展示用）</summary>
    public List<string> EquipmentTypeNames { get; set; } = new();
    /// <summary>适用技师技能分类 ID 列表（服务项目）</summary>
    public List<long> SkillCategoryIds { get; set; } = new();
    /// <summary>适用技师技能分类名称列表（服务项目，展示用）</summary>
    public List<string> SkillCategoryNames { get; set; } = new();
}

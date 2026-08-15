namespace Bms.Store.Application.Dtos.Technicians;

/// <summary>
/// 技师可服务项目条目（技师页展示擅长项目，双向匹配展示用）
/// </summary>
public class TechnicianServiceItemDto
{
    /// <summary>
    /// 服务项目子表ID（ServiceProduct.Id）
    /// </summary>
    public long ServiceProductId { get; set; }

    /// <summary>
    /// 商品主档ID（ProductMaster.Id）
    /// </summary>
    public long ProductId { get; set; }

    /// <summary>
    /// 服务项目名称（商品主档名称）
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 服务时长（分钟）
    /// </summary>
    public int? Duration { get; set; }

    /// <summary>
    /// 该服务项目在门店配置的适用技能分类ID列表
    /// </summary>
    public List<long> SkillCategoryIds { get; set; } = new();

    /// <summary>
    /// 该服务项目在门店配置的适用技能分类名称列表（展示用）
    /// </summary>
    public List<string> SkillCategoryNames { get; set; } = new();
}

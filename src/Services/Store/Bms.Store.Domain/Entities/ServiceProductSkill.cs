namespace Bms.Store.Domain.Entities;

/// <summary>
/// 服务项目-技能分类 关联表
/// 服务项目(租户级)的"适用技师技能"按门店语境配置（门店级隔离，StoreId 归属）
/// 与技师技能标签(TechnicianSkill)同口径，用于"服务项目 ↔ 技师"技能弱关联匹配
/// 树形匹配：双方各自展开为"自身+所有后代"，两集合交集非空即匹配（选父级不遗漏子级）
/// </summary>
public class ServiceProductSkill : StoreBusinessEntityBase
{
    /// <summary>
    /// 服务项目子表 ID（关联 ServiceProduct.Id）
    /// </summary>
    public long ServiceProductId { get; set; }

    /// <summary>
    /// 技能分类 ID（关联 SkillCategory.Id，当前门店语境）
    /// </summary>
    public long SkillCategoryId { get; set; }

    /// <summary>
    /// 导航属性：服务项目
    /// </summary>
    public ServiceProduct? ServiceProduct { get; set; }

    /// <summary>
    /// 导航属性：技能分类
    /// </summary>
    public SkillCategory? SkillCategory { get; set; }
}

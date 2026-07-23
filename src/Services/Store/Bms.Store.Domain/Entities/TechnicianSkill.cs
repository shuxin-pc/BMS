namespace Bms.Store.Domain.Entities;

/// <summary>
/// 技师-技能分类 关联表
/// 对应需求 B4.3：技师技能标签结构化关联（替代 Technician.SkillTags 字符串字段）
/// </summary>
public class TechnicianSkill : StoreEntity
{
    /// <summary>
    /// 技师 ID（关联 Technician.Id）
    /// </summary>
    public long TechnicianId { get; set; }

    /// <summary>
    /// 技能分类 ID（关联 SkillCategory.Id）
    /// </summary>
    public long SkillCategoryId { get; set; }

    /// <summary>
    /// 熟练度等级（1-5，null=未评级）
    /// </summary>
    public int? ProficiencyLevel { get; set; }

    /// <summary>
    /// 导航属性：技师
    /// </summary>
    public Technician? Technician { get; set; }

    /// <summary>
    /// 导航属性：技能分类
    /// </summary>
    public SkillCategory? SkillCategory { get; set; }
}

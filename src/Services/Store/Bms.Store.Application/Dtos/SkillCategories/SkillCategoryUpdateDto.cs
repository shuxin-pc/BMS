namespace Bms.Store.Application.Dtos.SkillCategories;

/// <summary>
/// 更新技能分类请求 DTO
/// </summary>
public class SkillCategoryUpdateDto : SkillCategoryCreateDto
{
    /// <summary>
    /// 分类ID
    /// </summary>
    public long Id { get; set; }
}

using Bms.Store.Application.Dtos;

namespace Bms.Store.Application.Dtos.SkillCategories;

/// <summary>
/// 技能分类分页查询 DTO
/// </summary>
public class SkillCategoryQueryDto : PagedRequestDto
{
    /// <summary>
    /// 分类名称（模糊匹配）
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// 分类编码（模糊匹配）
    /// </summary>
    public string? Code { get; set; }

    /// <summary>
    /// 父分类ID
    /// </summary>
    public long? ParentId { get; set; }
}

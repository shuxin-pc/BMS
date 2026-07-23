namespace Bms.Store.Application.Dtos.SkillCategories;

/// <summary>
/// 创建技能分类请求 DTO
/// </summary>
public class SkillCategoryCreateDto
{
    /// <summary>
    /// 分类名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 分类编码
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 父分类ID（null=顶级分类）
    /// </summary>
    public long? ParentId { get; set; }

    /// <summary>
    /// 状态（0:禁用 1:启用）
    /// </summary>
    public int Status { get; set; } = 1;

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}

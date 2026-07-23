namespace Bms.Store.Application.Dtos.SkillCategories;

/// <summary>
/// 技能分类 DTO
/// </summary>
public class SkillCategoryDto
{
    /// <summary>
    /// 分类ID
    /// </summary>
    public long Id { get; set; }

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
    public int Status { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}

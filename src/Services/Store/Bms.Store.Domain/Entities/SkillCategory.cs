namespace Bms.Store.Domain.Entities;

/// <summary>
/// 技能分类
/// 技能分类由平台统一定义，支持层级结构
/// </summary>
public class SkillCategory : StoreEntity
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
    /// 父分类ID（支持层级结构，null=顶级分类）
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

    /// <summary>
    /// 导航属性：父分类
    /// </summary>
    public SkillCategory? Parent { get; set; }
}

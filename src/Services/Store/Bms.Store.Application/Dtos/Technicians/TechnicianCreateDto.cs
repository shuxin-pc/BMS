namespace Bms.Store.Application.Dtos.Technicians;

/// <summary>
/// 创建商家技师请求 DTO
/// </summary>
public class TechnicianCreateDto
{
    /// <summary>
    /// 技师姓名
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 手机号
    /// </summary>
    public string Phone { get; set; } = string.Empty;

    /// <summary>
    /// 性别（0:未知 1:男 2:女）
    /// </summary>
    public int Gender { get; set; }

    /// <summary>
    /// 技能分类ID列表
    /// </summary>
    public List<long> SkillCategoryIds { get; set; } = new();

    /// <summary>
    /// 头像URL
    /// </summary>
    public string? AvatarUrl { get; set; }

    /// <summary>
    /// 状态（0:禁用 1:启用）
    /// </summary>
    public int Status { get; set; } = 1;

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}

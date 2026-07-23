namespace Bms.Store.Domain.Entities;

/// <summary>
/// 商家技师
/// </summary>
public class Technician : StoreEntity
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
    /// 头像URL
    /// </summary>
    public string? AvatarUrl { get; set; }

    /// <summary>
    /// 状态（0:禁用 1:启用）
    /// </summary>
    public int Status { get; set; } = 1;

    /// <summary>
    /// 技师来源（1:商家技师 2:平台技师）
    /// </summary>
    public int Source { get; set; } = 1;

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 导航属性：技师技能标签关联
    /// </summary>
    public List<TechnicianSkill> TechnicianSkills { get; set; } = new();
}

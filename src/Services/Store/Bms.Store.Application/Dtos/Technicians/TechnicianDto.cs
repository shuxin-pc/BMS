namespace Bms.Store.Application.Dtos.Technicians;

/// <summary>
/// 商家技师 DTO
/// </summary>
public class TechnicianDto
{
    public long Id { get; set; }

    /// <summary>
    /// 技师归属租户ID（前端据此判断当前租户是否可编辑/删除）
    /// </summary>
    public long TenantId { get; set; }

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
    /// 技能分类名称列表
    /// </summary>
    public List<string> SkillCategoryNames { get; set; } = new();

    /// <summary>
    /// 头像URL
    /// </summary>
    public string? AvatarUrl { get; set; }

    /// <summary>
    /// 状态（1:在岗 2:休息）
    /// </summary>
    public int Status { get; set; }

    /// <summary>
    /// 技师来源（1:商家技师 2:平台技师）
    /// </summary>
    public int Source { get; set; }

    /// <summary>
    /// 技师来源文本（"自有"/"平台"），由 Source 派生，前端直接展示
    /// </summary>
    public string SourceText => Source == 2 ? "平台" : "自有";

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

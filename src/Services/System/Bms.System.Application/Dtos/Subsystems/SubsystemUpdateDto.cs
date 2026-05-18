namespace Bms.System.Application.Dtos.Subsystems;

/// <summary>
/// 子系统更新 DTO
/// </summary>
public class SubsystemUpdateDto
{
    /// <summary>
    /// 子系统ID
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 子系统名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 图标
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// 描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 排序号
    /// </summary>
    public int Sort { get; set; } = 0;

    /// <summary>
    /// 状态：0-禁用，1-启用
    /// </summary>
    public int Status { get; set; } = 1;
}

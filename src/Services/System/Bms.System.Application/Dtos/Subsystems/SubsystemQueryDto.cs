namespace Bms.System.Application.Dtos.Subsystems;

/// <summary>
/// 子系统查询 DTO
/// </summary>
public class SubsystemQueryDto
{
    /// <summary>
    /// 子系统名称（模糊搜索）
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// 子系统编码（模糊搜索）
    /// </summary>
    public string? Code { get; set; }

    /// <summary>
    /// 状态：0-禁用，1-启用（null-全部）
    /// </summary>
    public int? Status { get; set; }
}

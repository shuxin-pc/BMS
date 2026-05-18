namespace Bms.System.Application.Dtos.Subsystems;

/// <summary>
/// 子系统菜单分配 DTO
/// </summary>
public class SubsystemMenuAssignDto
{
    /// <summary>
    /// 菜单ID列表
    /// </summary>
    public List<long> MenuIds { get; set; } = new List<long>();
}

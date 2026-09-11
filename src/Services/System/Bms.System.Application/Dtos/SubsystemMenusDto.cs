using Bms.System.Application.Dtos.Menus;

namespace Bms.System.Application.Dtos;

/// <summary>
/// 子系统菜单树（当前用户在单个子系统下的授权菜单，供全局搜索功能源使用）
/// </summary>
public class SubsystemMenusDto
{
    /// <summary>
    /// 子系统ID
    /// </summary>
    public long SubsystemId { get; set; }

    /// <summary>
    /// 子系统名称
    /// </summary>
    public string SubsystemName { get; set; } = string.Empty;

    /// <summary>
    /// 该子系统的授权菜单树（含目录/页面，不含按钮）
    /// </summary>
    public List<MenuDto> Menus { get; set; } = new();
}

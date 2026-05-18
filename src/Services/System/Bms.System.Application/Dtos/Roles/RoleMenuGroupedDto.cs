using Bms.System.Application.Dtos.Menus;

namespace Bms.System.Application.Dtos.Roles;

/// <summary>
/// 按子系统分组的角色菜单权限 DTO
/// </summary>
public class RoleMenuGroupedDto
{
    /// <summary>
    /// 子系统ID
    /// </summary>
    public long SubsystemId { get; set; }

    /// <summary>
    /// 子系统编码
    /// </summary>
    public string SubsystemCode { get; set; } = string.Empty;

    /// <summary>
    /// 子系统名称
    /// </summary>
    public string SubsystemName { get; set; } = string.Empty;

    /// <summary>
    /// 子系统图标
    /// </summary>
    public string? SubsystemIcon { get; set; }

    /// <summary>
    /// 菜单树
    /// </summary>
    public List<MenuDto> Menus { get; set; } = new List<MenuDto>();

    /// <summary>
    /// 已选中的菜单ID列表
    /// </summary>
    public List<long> SelectedMenuIds { get; set; } = new List<long>();
}

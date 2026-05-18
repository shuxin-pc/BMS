namespace Bms.System.Application.Dtos.Roles;

/// <summary>
/// 角色菜单权限分配 DTO
/// </summary>
public class RoleMenuAssignDto
{
    /// <summary>
    /// 菜单ID列表
    /// </summary>
    public List<long> MenuIds { get; set; } = new List<long>();
}

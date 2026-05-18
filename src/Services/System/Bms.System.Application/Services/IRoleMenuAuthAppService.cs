using Bms.System.Application.Dtos;
using Bms.System.Application.Dtos.Roles;

namespace Bms.System.Application.Services;

/// <summary>
/// 角色菜单权限应用服务接口
/// </summary>
public interface IRoleMenuAuthAppService
{
    Task<ApiResponseDto<List<long>>> GetByRoleIdAsync(long roleId);
    Task<ApiResponseDto<List<RoleMenuGroupedDto>>> GetGroupedByRoleIdAsync(long roleId, long? tenantId = null, bool isSuperAdmin = false);
    Task<ApiResponseDto> AssignMenusAsync(long roleId, RoleMenuAssignDto dto);
    Task<ApiResponseDto> RemoveMenuAsync(long roleId, long menuId);
}

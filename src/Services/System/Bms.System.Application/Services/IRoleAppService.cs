using Bms.System.Application.Dtos;
using Bms.System.Application.Dtos.Roles;

namespace Bms.System.Application.Services;

public interface IRoleAppService
{
    Task<ApiResponseDto<PagedResponseDto<RoleDto>>> GetPagedListAsync(PagedRequestDto request, bool isSuperAdmin = true, long? tenantId = null);
    Task<ApiResponseDto<List<RoleDto>>> GetAllListAsync(bool isSuperAdmin = true, long? tenantId = null);
    Task<ApiResponseDto<List<RoleDto>>> GetAllListWithoutFilterAsync();
    Task<ApiResponseDto<RoleDto?>> GetByIdAsync(long id);
    Task<ApiResponseDto<RoleDto>> CreateAsync(RoleCreateDto dto, long currentTenantId, string currentTenantCode);
    Task<ApiResponseDto<RoleDto>> UpdateAsync(RoleUpdateDto dto);
    Task<ApiResponseDto> DeleteAsync(long id);
    Task<ApiResponseDto> BatchDeleteAsync(List<long> ids);
    Task<ApiResponseDto<List<Dtos.Menus.MenuDto>>> GetRoleMenusAsync(long roleId);
}
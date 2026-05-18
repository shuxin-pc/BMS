using Bms.System.Application.Dtos;
using Bms.System.Application.Dtos.DataPermissions;

namespace Bms.System.Application.Services;

public interface IDataPermissionAppService
{
    Task<ApiResponseDto<DataPermissionDto?>> GetByIdAsync(long id);
    Task<ApiResponseDto<DataPermissionDto?>> GetByRoleIdAsync(long roleId);
    Task<ApiResponseDto<List<DataPermissionDto>>> GetListAsync();
    Task<ApiResponseDto<DataPermissionDto>> CreateAsync(DataPermissionCreateDto dto);
    Task<ApiResponseDto<DataPermissionDto>> UpdateAsync(DataPermissionUpdateDto dto);
    Task<ApiResponseDto> DeleteAsync(long id);
    Task<ApiResponseDto> DeleteByRoleIdAsync(long roleId);
}

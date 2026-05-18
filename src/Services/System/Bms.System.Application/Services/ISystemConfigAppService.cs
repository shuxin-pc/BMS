using Bms.System.Application.Dtos;
using Bms.System.Application.Dtos.SystemConfigs;

namespace Bms.System.Application.Services;

public interface ISystemConfigAppService
{
    Task<ApiResponseDto<SystemConfigDto?>> GetByIdAsync(long id);
    Task<ApiResponseDto<SystemConfigDto?>> GetByKeyAsync(string configKey);
    Task<ApiResponseDto<List<SystemConfigDto>>> GetListAsync();
    Task<ApiResponseDto<PagedResponseDto<SystemConfigDto>>> GetPagedListAsync(PagedRequestDto request, SystemConfigQueryDto? query, long currentTenantId, bool isSuperAdmin);
    Task<ApiResponseDto<List<SystemConfigDto>>> GetByGroupAsync(string configGroup);
    Task<ApiResponseDto<List<SystemConfigDto>>> GetPublicConfigsAsync();
    Task<ApiResponseDto<SystemConfigDto>> CreateAsync(SystemConfigCreateDto dto, long currentTenantId, string currentTenantCode, bool isSuperAdmin);
    Task<ApiResponseDto<SystemConfigDto>> UpdateAsync(SystemConfigUpdateDto dto, long currentTenantId, string currentTenantCode, bool isSuperAdmin);
    Task<ApiResponseDto> DeleteAsync(long id, long currentTenantId, bool isSuperAdmin);
}

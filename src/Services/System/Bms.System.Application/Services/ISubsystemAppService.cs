using Bms.System.Application.Dtos;
using Bms.System.Application.Dtos.Subsystems;

namespace Bms.System.Application.Services;

/// <summary>
/// 子系统应用服务接口
/// </summary>
public interface ISubsystemAppService
{
    Task<ApiResponseDto<List<SubsystemDto>>> GetListAsync(SubsystemQueryDto query, bool isSuperAdmin = true, long? tenantId = null);
    Task<ApiResponseDto<List<SubsystemDto>>> GetAllEnabledAsync(bool isSuperAdmin = true, long? tenantId = null);
    Task<ApiResponseDto<List<SubsystemDto>>> GetAllAsync(bool isSuperAdmin = true, long? tenantId = null);
    Task<ApiResponseDto<SubsystemDto?>> GetByIdAsync(long id);
    Task<ApiResponseDto<int>> GetUsageCountAsync(long id);
    Task<ApiResponseDto<SubsystemDto>> CreateAsync(SubsystemCreateDto dto);
    Task<ApiResponseDto<SubsystemDto>> UpdateAsync(SubsystemUpdateDto dto);
    Task<ApiResponseDto> DeleteAsync(long id);
    Task<ApiResponseDto<List<long>>> GetMenusAsync(long id);
    Task<ApiResponseDto> AssignMenusAsync(long id, SubsystemMenuAssignDto dto);
}

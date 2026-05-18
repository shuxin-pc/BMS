using Bms.System.Application.Dtos;
using Bms.System.Application.Dtos.Tenants;

namespace Bms.System.Application.Services;

/// <summary>
/// 租户子系统应用服务接口
/// </summary>
public interface ITenantSubsystemAppService
{
    Task<ApiResponseDto<List<long>>> GetByTenantIdAsync(long tenantId);
    Task<ApiResponseDto> AssignSubsystemsAsync(long tenantId, TenantSubsystemAssignDto dto);
    Task<ApiResponseDto> AddSubsystemAsync(long tenantId, long subsystemId);
    Task<ApiResponseDto> RemoveSubsystemAsync(long tenantId, long subsystemId);
    Task<ApiResponseDto> BatchAddSubsystemsAsync(long tenantId, List<long> subsystemIds);
    Task<ApiResponseDto> BatchRemoveSubsystemsAsync(long tenantId, List<long> subsystemIds);
}

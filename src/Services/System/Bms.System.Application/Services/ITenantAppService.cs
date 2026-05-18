using Bms.System.Application.Dtos;
using Bms.System.Application.Dtos.Tenants;

namespace Bms.System.Application.Services;

/// <summary>
/// 租户管理应用服务接口
/// </summary>
public interface ITenantAppService
{
    Task<ApiResponseDto<PagedResponseDto<TenantDto>>> GetPagedListAsync(PagedRequestDto request);
    Task<ApiResponseDto<TenantDto?>> GetByIdAsync(long id);
    Task<ApiResponseDto<TenantDto?>> GetByCodeAsync(string code);
    Task<ApiResponseDto<TenantDto>> CreateAsync(TenantCreateDto dto);
    Task<ApiResponseDto<TenantDto>> UpdateAsync(TenantUpdateDto dto);
    Task<ApiResponseDto> DeleteAsync(long id);
    Task<ApiResponseDto> BatchDeleteAsync(List<long> ids);
    Task<ApiResponseDto> EnableAsync(long id);
    Task<ApiResponseDto> DisableAsync(long id);
}

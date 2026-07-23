using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Technicians;

namespace Bms.Store.Application.Services;

/// <summary>
/// 商家技师应用服务接口
/// </summary>
public interface ITechnicianAppService
{
    Task<ApiResponseDto<PagedResponseDto<TechnicianDto>>> GetPagedListAsync(TechnicianQueryDto query);
    Task<ApiResponseDto<TechnicianDto?>> GetByIdAsync(long id);
    Task<ApiResponseDto<TechnicianDto>> CreateAsync(TechnicianCreateDto dto);
    Task<ApiResponseDto<TechnicianDto>> UpdateAsync(TechnicianUpdateDto dto);
    Task<ApiResponseDto> DeleteAsync(long id);
    Task<ApiResponseDto> BatchDeleteAsync(List<long> ids);
}

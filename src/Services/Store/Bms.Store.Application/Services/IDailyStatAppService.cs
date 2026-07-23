using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Statistics;

namespace Bms.Store.Application.Services;

/// <summary>
/// 日统计应用服务接口
/// </summary>
public interface IDailyStatAppService
{
    Task<ApiResponseDto<PagedResponseDto<DailyStatDto>>> GetPagedListAsync(DailyStatQueryDto query);
    Task<ApiResponseDto<DailyStatDto?>> GetByIdAsync(long id);
    Task<ApiResponseDto<DailyStatDto>> CreateAsync(DailyStatCreateDto dto);
    Task<ApiResponseDto<DailyStatDto>> UpdateAsync(DailyStatUpdateDto dto);
    Task<ApiResponseDto> DeleteAsync(long id);
    Task<ApiResponseDto> BatchDeleteAsync(List<long> ids);
}

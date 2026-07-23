using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.PriceChangeLogs;

namespace Bms.Store.Application.Services;

/// <summary>
/// 价格变更记录应用服务接口
/// </summary>
public interface IPriceChangeLogAppService
{
    Task<ApiResponseDto<PagedResponseDto<PriceChangeLogDto>>> GetPagedListAsync(PriceChangeLogQueryDto query);
    Task<ApiResponseDto<PriceChangeLogDto?>> GetByIdAsync(long id);
    Task<ApiResponseDto<PriceChangeLogDto>> CreateAsync(PriceChangeLogCreateDto dto);
    Task<ApiResponseDto<PriceChangeLogDto>> UpdateAsync(PriceChangeLogUpdateDto dto);
    Task<ApiResponseDto> DeleteAsync(long id);
    Task<ApiResponseDto> BatchDeleteAsync(List<long> ids);
    Task<ApiResponseDto<BatchPriceAdjustResultDto>> BatchAdjustPriceAsync(BatchPriceAdjustDto dto);
}

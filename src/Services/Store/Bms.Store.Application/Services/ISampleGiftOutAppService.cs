using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.SampleGifts;

namespace Bms.Store.Application.Services;

/// <summary>
/// 赠品出库记录应用服务接口
/// </summary>
public interface ISampleGiftOutAppService
{
    Task<ApiResponseDto<PagedResponseDto<SampleGiftOutDto>>> GetPagedListAsync(SampleGiftOutQueryDto query);
    Task<ApiResponseDto<SampleGiftOutDto?>> GetByIdAsync(long id);
    Task<ApiResponseDto<SampleGiftOutDto>> CreateAsync(SampleGiftOutCreateDto dto);
    Task<ApiResponseDto<SampleGiftOutDto>> UpdateAsync(SampleGiftOutUpdateDto dto);
    Task<ApiResponseDto> DeleteAsync(long id);
    Task<ApiResponseDto> BatchDeleteAsync(List<long> ids);
}

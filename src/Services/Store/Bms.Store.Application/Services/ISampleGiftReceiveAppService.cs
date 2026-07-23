using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.SampleGifts;

namespace Bms.Store.Application.Services;

/// <summary>
/// 样品领用记录应用服务接口
/// </summary>
public interface ISampleGiftReceiveAppService
{
    Task<ApiResponseDto<PagedResponseDto<SampleGiftReceiveDto>>> GetPagedListAsync(SampleGiftReceiveQueryDto query);
    Task<ApiResponseDto<SampleGiftReceiveDto?>> GetByIdAsync(long id);
    Task<ApiResponseDto<SampleGiftReceiveDto>> CreateAsync(SampleGiftReceiveCreateDto dto);
    Task<ApiResponseDto<SampleGiftReceiveDto>> UpdateAsync(SampleGiftReceiveUpdateDto dto);
    Task<ApiResponseDto> DeleteAsync(long id);
    Task<ApiResponseDto> BatchDeleteAsync(List<long> ids);
}

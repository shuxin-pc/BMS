using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.TreatmentCards;

namespace Bms.Store.Application.Services;

/// <summary>
/// 疗程卡配置应用服务接口
/// </summary>
public interface ITreatmentCardAppService
{
    Task<ApiResponseDto<PagedResponseDto<TreatmentCardDto>>> GetPagedListAsync(TreatmentCardQueryDto query);
    Task<ApiResponseDto<TreatmentCardDto?>> GetByIdAsync(long id);
    Task<ApiResponseDto<TreatmentCardDto>> CreateAsync(TreatmentCardCreateDto dto);
    Task<ApiResponseDto<TreatmentCardDto>> UpdateAsync(TreatmentCardUpdateDto dto);
    Task<ApiResponseDto> DeleteAsync(long id);
    Task<ApiResponseDto> BatchDeleteAsync(List<long> ids);
}

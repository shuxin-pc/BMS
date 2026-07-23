using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Customers;

namespace Bms.Store.Application.Services;

/// <summary>
/// 消费记录应用服务接口
/// </summary>
public interface IConsumeLogAppService
{
    Task<ApiResponseDto<PagedResponseDto<ConsumeLogDto>>> GetPagedListAsync(ConsumeLogQueryDto query);
    Task<ApiResponseDto<ConsumeLogDto?>> GetByIdAsync(long id);
    Task<ApiResponseDto<ConsumeLogDto>> CreateAsync(ConsumeLogCreateDto dto);
    Task<ApiResponseDto<ConsumeLogDto>> UpdateAsync(ConsumeLogUpdateDto dto);
    Task<ApiResponseDto> DeleteAsync(long id);
    Task<ApiResponseDto> BatchDeleteAsync(List<long> ids);
}

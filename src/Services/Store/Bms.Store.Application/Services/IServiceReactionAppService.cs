using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Customers;

namespace Bms.Store.Application.Services;

public interface IServiceReactionAppService
{
    Task<ApiResponseDto<PagedResponseDto<ServiceReactionDto>>> GetPagedListAsync(ServiceReactionQueryDto query);
    Task<ApiResponseDto<ServiceReactionDto?>> GetByIdAsync(long id);
    Task<ApiResponseDto<ServiceReactionDto>> CreateAsync(ServiceReactionCreateDto dto);
    Task<ApiResponseDto<ServiceReactionDto>> UpdateAsync(ServiceReactionUpdateDto dto);
    Task<ApiResponseDto> DeleteAsync(long id);
    Task<ApiResponseDto> BatchDeleteAsync(List<long> ids);
}

using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Customers;

namespace Bms.Store.Application.Services;

public interface ICustomerPreferenceAppService
{
    Task<ApiResponseDto<PagedResponseDto<CustomerPreferenceDto>>> GetPagedListAsync(CustomerPreferenceQueryDto query);
    Task<ApiResponseDto<CustomerPreferenceDto?>> GetByIdAsync(long id);
    Task<ApiResponseDto<CustomerPreferenceDto>> CreateAsync(CustomerPreferenceCreateDto dto);
    Task<ApiResponseDto<CustomerPreferenceDto>> UpdateAsync(CustomerPreferenceUpdateDto dto);
    Task<ApiResponseDto> DeleteAsync(long id);
    Task<ApiResponseDto> BatchDeleteAsync(List<long> ids);
}

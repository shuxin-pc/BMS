using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Customers;

namespace Bms.Store.Application.Services;

/// <summary>
/// 客户等级应用服务接口
/// </summary>
public interface ICustomerLevelAppService
{
    Task<ApiResponseDto<PagedResponseDto<CustomerLevelDto>>> GetPagedListAsync(CustomerLevelQueryDto query);
    Task<ApiResponseDto<CustomerLevelDto?>> GetByIdAsync(long id);
    Task<ApiResponseDto<CustomerLevelDto>> CreateAsync(CustomerLevelCreateDto dto);
    Task<ApiResponseDto<CustomerLevelDto>> UpdateAsync(CustomerLevelUpdateDto dto);
    Task<ApiResponseDto> DeleteAsync(long id);
    Task<ApiResponseDto> BatchDeleteAsync(List<long> ids);
}

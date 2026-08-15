using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Customers;

namespace Bms.Store.Application.Services;

/// <summary>
/// 客户标签应用服务接口
/// 标签字典按门店隔离，各门店独立维护
/// </summary>
public interface ICustomerTagAppService
{
    Task<ApiResponseDto<PagedResponseDto<CustomerTagDto>>> GetPagedListAsync(CustomerTagQueryDto query);
    Task<ApiResponseDto<List<CustomerTagDto>>> GetAllAsync();
    Task<ApiResponseDto<CustomerTagDto?>> GetByIdAsync(long id);
    Task<ApiResponseDto<CustomerTagDto>> CreateAsync(CustomerTagCreateDto dto);
    Task<ApiResponseDto<CustomerTagDto>> UpdateAsync(CustomerTagUpdateDto dto);
    Task<ApiResponseDto> DeleteAsync(long id);
    Task<ApiResponseDto> BatchDeleteAsync(List<long> ids);
}

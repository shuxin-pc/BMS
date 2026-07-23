using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Customers;

namespace Bms.Store.Application.Services;

/// <summary>
/// 客户美容档案应用服务接口
/// </summary>
public interface ICustomerBeautyProfileAppService
{
    Task<ApiResponseDto<PagedResponseDto<CustomerBeautyProfileDto>>> GetPagedListAsync(CustomerBeautyProfileQueryDto query);
    Task<ApiResponseDto<CustomerBeautyProfileDto?>> GetByIdAsync(long id);
    Task<ApiResponseDto<CustomerBeautyProfileDto>> CreateAsync(CustomerBeautyProfileCreateDto dto);
    Task<ApiResponseDto<CustomerBeautyProfileDto>> UpdateAsync(CustomerBeautyProfileUpdateDto dto);
    Task<ApiResponseDto> DeleteAsync(long id);
    Task<ApiResponseDto> BatchDeleteAsync(List<long> ids);
}

using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Products;

namespace Bms.Store.Application.Services;

/// <summary>
/// 服务商品子表应用服务接口
/// </summary>
public interface IServiceProductAppService
{
    Task<ApiResponseDto<PagedResponseDto<ServiceProductDto>>> GetPagedListAsync(ServiceProductQueryDto query);
    Task<ApiResponseDto<ServiceProductDto?>> GetByIdAsync(long id);
    Task<ApiResponseDto<ServiceProductDto>> CreateAsync(ServiceProductCreateDto dto);
    Task<ApiResponseDto<ServiceProductDto>> UpdateAsync(ServiceProductUpdateDto dto);
    Task<ApiResponseDto> DeleteAsync(long id);
    Task<ApiResponseDto> BatchDeleteAsync(List<long> ids);
}

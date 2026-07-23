using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Statistics;

namespace Bms.Store.Application.Services;

/// <summary>
/// 商品销售统计应用服务接口
/// </summary>
public interface IProductSalesStatAppService
{
    Task<ApiResponseDto<PagedResponseDto<ProductSalesStatDto>>> GetPagedListAsync(ProductSalesStatQueryDto query);
    Task<ApiResponseDto<ProductSalesStatDto?>> GetByIdAsync(long id);
    Task<ApiResponseDto<ProductSalesStatDto>> CreateAsync(ProductSalesStatCreateDto dto);
    Task<ApiResponseDto<ProductSalesStatDto>> UpdateAsync(ProductSalesStatUpdateDto dto);
    Task<ApiResponseDto> DeleteAsync(long id);
    Task<ApiResponseDto> BatchDeleteAsync(List<long> ids);
}

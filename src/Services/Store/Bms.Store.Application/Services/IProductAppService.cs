using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Products;
using Bms.Store.Application.Dtos.Suppliers;

namespace Bms.Store.Application.Services;

/// <summary>
/// 商品档案应用服务接口
/// </summary>
public interface IProductAppService
{
    Task<ApiResponseDto<PagedResponseDto<ProductDto>>> GetPagedListAsync(ProductQueryDto query);
    Task<ApiResponseDto<ProductDto?>> GetByIdAsync(long id);
    Task<ApiResponseDto<ProductDto>> CreateAsync(ProductCreateDto dto);
    Task<ApiResponseDto<ProductDto>> UpdateAsync(ProductUpdateDto dto);
    Task<ApiResponseDto> DeleteAsync(long id);
    Task<ApiResponseDto> BatchDeleteAsync(List<long> ids);

    // ========== 品项-供应商关联（G2.6.2）==========

    /// <summary>
    /// 查询品项关联的供应商列表（按 IsDefault 倒序，默认供应商排在首位）
    /// </summary>
    Task<ApiResponseDto<List<ProductSupplierDto>>> GetSuppliersByProductAsync(long productId);

    /// <summary>
    /// 获取商品轻量选项列表（不分页，仅返回 Id/Name/Code/Unit，用于下拉选择场景）
    /// </summary>
    Task<ApiResponseDto<List<ProductOptionDto>>> GetOptionsAsync();
}

/// <summary>
/// 商品分类应用服务接口
/// </summary>
public interface IProductCategoryAppService
{
    Task<ApiResponseDto<List<ProductCategoryDto>>> GetTreeAsync();
    Task<ApiResponseDto<ProductCategoryDto>> CreateAsync(ProductCategoryCreateDto dto);
    Task<ApiResponseDto<ProductCategoryDto>> UpdateAsync(ProductCategoryUpdateDto dto);
    Task<ApiResponseDto> DeleteAsync(long id);
}

using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Suppliers;

namespace Bms.Store.Application.Services;

/// <summary>
/// 供应商应用服务接口
/// </summary>
public interface ISupplierAppService
{
    Task<ApiResponseDto<PagedResponseDto<SupplierDto>>> GetPagedListAsync(SupplierQueryDto query);
    Task<ApiResponseDto<SupplierDto?>> GetByIdAsync(long id);
    Task<ApiResponseDto<SupplierDto>> CreateAsync(SupplierCreateDto dto);
    Task<ApiResponseDto<SupplierDto>> UpdateAsync(SupplierUpdateDto dto);
    Task<ApiResponseDto> DeleteAsync(long id);
    Task<ApiResponseDto> BatchDeleteAsync(List<long> ids);

    // ========== 供应商-品项关联（G2.6.2）==========

    /// <summary>
    /// 批量绑定品项到供应商（已存在的关联将跳过，自动写入参考价与供货周期）
    /// </summary>
    Task<ApiResponseDto<List<ProductSupplierDto>>> BindProductsAsync(BindProductsDto dto);

    /// <summary>
    /// 解除品项与供应商的关联（若解除的是默认供应商，自动将下一个关联设为新默认）
    /// </summary>
    Task<ApiResponseDto> UnbindProductAsync(long productId, long supplierId);

    /// <summary>
    /// 设置品项的默认供应商（自动取消旧默认，可选更新参考价与供货周期）
    /// </summary>
    Task<ApiResponseDto<ProductSupplierDto>> SetDefaultSupplierAsync(SetDefaultSupplierDto dto);

    /// <summary>
    /// 查询供应商关联的品项列表
    /// </summary>
    Task<ApiResponseDto<List<ProductSupplierDto>>> GetProductsBySupplierAsync(long supplierId);

    /// <summary>
    /// 获取供应商轻量选项列表（不分页，仅返回 Id/Name，用于下拉选择场景）
    /// </summary>
    Task<ApiResponseDto<List<SupplierOptionDto>>> GetOptionsAsync();
}

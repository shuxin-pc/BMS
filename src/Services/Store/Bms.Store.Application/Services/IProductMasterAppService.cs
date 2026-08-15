using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Products;

namespace Bms.Store.Application.Services;

/// <summary>
/// 商品主档应用服务接口（租户级，承载商品本质属性）
/// 对应设计文档 6.1/6.2/7.2 节
/// Master 字段全租户生效，Store 字段通过 BatchConfigStoreFieldsAsync 统一配置
/// </summary>
public interface IProductMasterAppService
{
    /// <summary>获取商品主档分页列表（租户级，含子表字段）</summary>
    Task<ApiResponseDto<PagedResponseDto<ProductMasterDto>>> GetPagedListAsync(ProductMasterQueryDto query);

    /// <summary>根据ID获取商品主档详情（含子表字段）</summary>
    Task<ApiResponseDto<ProductMasterDto?>> GetByIdAsync(long id);

    /// <summary>创建商品主档（服务商品同时创建 ServiceProduct 子表）</summary>
    Task<ApiResponseDto<ProductMasterDto>> CreateAsync(ProductMasterCreateDto dto);

    /// <summary>更新商品主档（Master 字段全租户生效，含子表处理）</summary>
    Task<ApiResponseDto<ProductMasterDto>> UpdateAsync(ProductMasterUpdateDto dto);

    /// <summary>
    /// 删除商品主档（基础版：软删除 Master + 关联 Product）
    /// 完整删除策略（库存检查 + 级联子表）在 P3.6 实现
    /// </summary>
    Task<ApiResponseDto> DeleteAsync(long id);

    /// <summary>
    /// 统一配置门店档案 Store 字段（对应设计文档 7.2/7.3 节）
    /// 将 Store 字段值应用到选中门店：已有 Product -> 覆盖；无 Product -> 自动创建
    /// </summary>
    Task<ApiResponseDto> BatchConfigStoreFieldsAsync(ProductStoreBatchConfigDto dto);

    /// <summary>
    /// 预览门店档案配置（对应设计文档 7.3 节二次确认）
    /// 查询选中门店中哪些已存在该主档的 Product 档案（将被覆盖），哪些无档案（将新建）
    /// </summary>
    /// <param name="masterId">商品主档ID</param>
    /// <param name="storeIds">待应用的门店ID列表</param>
    Task<ApiResponseDto<ProductStoreConfigPreviewDto>> GetStoreConfigPreviewAsync(long masterId, List<long> storeIds);

    /// <summary>获取商品主档轻量选项列表（下拉选择用）</summary>
    Task<ApiResponseDto<List<ProductMasterOptionDto>>> GetOptionsAsync();
}

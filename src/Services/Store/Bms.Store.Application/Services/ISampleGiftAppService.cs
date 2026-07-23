using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Products;
using Bms.Store.Application.Dtos.SampleGifts;

namespace Bms.Store.Application.Services;

/// <summary>
/// 样品/赠品档案应用服务接口
/// 样品/赠品即 Product 表中 Type=4（样品）或 Type=5（赠品）的记录
/// 提供档案管理 CRUD、库存查询、统计报表功能
/// </summary>
public interface ISampleGiftAppService
{
    /// <summary>
    /// 获取样品/赠品分页列表（Type=4或5）
    /// </summary>
    Task<ApiResponseDto<PagedResponseDto<ProductDto>>> GetPagedListAsync(SampleGiftQueryDto query);

    /// <summary>
    /// 根据ID获取样品/赠品详情
    /// </summary>
    Task<ApiResponseDto<ProductDto?>> GetByIdAsync(long id);

    /// <summary>
    /// 创建样品/赠品（强制 Type=4或5）
    /// </summary>
    Task<ApiResponseDto<ProductDto>> CreateAsync(ProductCreateDto dto);

    /// <summary>
    /// 更新样品/赠品
    /// </summary>
    Task<ApiResponseDto<ProductDto>> UpdateAsync(ProductUpdateDto dto);

    /// <summary>
    /// 删除样品/赠品（软删除）
    /// </summary>
    Task<ApiResponseDto> DeleteAsync(long id);

    /// <summary>
    /// 批量删除样品/赠品（软删除）
    /// </summary>
    Task<ApiResponseDto> BatchDeleteAsync(List<long> ids);

    /// <summary>
    /// 获取样品/赠品库存查询分页列表
    /// </summary>
    Task<ApiResponseDto<PagedResponseDto<SampleInventoryDto>>> GetInventoryListAsync(SampleInventoryQueryDto query);

    /// <summary>
    /// 获取样品/赠品统计报表分页列表
    /// </summary>
    Task<ApiResponseDto<PagedResponseDto<SampleReportDto>>> GetReportListAsync(SampleReportQueryDto query);

    /// <summary>
    /// 获取样品/赠品按活动维度统计报表分页列表（P-SG-04）
    /// 数据源：SampleGiftOut（赠品出库），按 ActivityId + ProductId 聚合
    /// </summary>
    Task<ApiResponseDto<PagedResponseDto<SampleActivityReportDto>>> GetReportByActivityAsync(SampleActivityReportQueryDto query);

    /// <summary>
    /// 获取样品/赠品按客户维度统计报表分页列表（P-SG-04）
    /// 数据源：SampleGiftReceive（样品领用），按 CustomerId 聚合
    /// </summary>
    Task<ApiResponseDto<PagedResponseDto<SampleCustomerReportDto>>> GetReportByCustomerAsync(SampleCustomerReportQueryDto query);
}

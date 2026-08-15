using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Products;
using Bms.Store.Application.Dtos.SampleGifts;

namespace Bms.Store.Application.Services;

/// <summary>
/// 样品/赠品档案应用服务接口
/// 样品/赠品即 Product 表中 Type=4（样品）或 Type=5（赠品）的记录
/// 提供统计报表功能（档案管理与库存查询已统一至 ProductMaster/Product/Inventory 体系）
/// </summary>
public interface ISampleGiftAppService
{
    /// <summary>
    /// 获取样品/赠品分页列表（Type=4或5）
    /// </summary>
    Task<ApiResponseDto<PagedResponseDto<ProductDto>>> GetPagedListAsync(SampleGiftQueryDto query);

    /// <summary>
    /// 获取样品/赠品统计报表分页列表
    /// </summary>
    Task<ApiResponseDto<PagedResponseDto<SampleReportDto>>> GetReportListAsync(SampleReportQueryDto query);

    /// <summary>
    /// 获取样品/赠品按活动维度统计报表分页列表（P-SG-04）
    /// R5：数据源 InventoryLogs（SourceType 9=样品领用出库/10=赠品活动出库），按 ActivityId + ProductId 聚合
    /// </summary>
    Task<ApiResponseDto<PagedResponseDto<SampleActivityReportDto>>> GetReportByActivityAsync(SampleActivityReportQueryDto query);

    /// <summary>
    /// 获取样品/赠品按客户维度统计报表分页列表（P-SG-04）
    /// 数据源：SampleGiftReceive（样品领用），按 CustomerId 聚合
    /// </summary>
    Task<ApiResponseDto<PagedResponseDto<SampleCustomerReportDto>>> GetReportByCustomerAsync(SampleCustomerReportQueryDto query);
}

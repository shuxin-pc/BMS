using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Products;
using Bms.Store.Application.Dtos.SampleGifts;
using Bms.Store.Application.Services;

namespace Bms.Store.Api.Controllers;

/// <summary>
/// 样品/赠品档案管理控制器
/// 样品/赠品即 Product 表中 Type=4（样品）或 Type=5（赠品）的记录
/// 提供统计报表接口（档案管理与库存查询已统一至 ProductMaster/Product/Inventory 体系）
/// </summary>
[ApiController]
[Route("api/store/[controller]")]
[Authorize]
public class SampleGiftsController : ControllerBase
{
    private readonly ISampleGiftAppService _appService;

    public SampleGiftsController(ISampleGiftAppService appService)
    {
        _appService = appService;
    }

    /// <summary>
    /// 获取样品/赠品分页列表
    /// </summary>
    [HttpGet]
    public async Task<ApiResponseDto<PagedResponseDto<ProductDto>>> GetList([FromQuery] SampleGiftQueryDto query)
        => await _appService.GetPagedListAsync(query);

    /// <summary>
    /// 获取样品/赠品统计报表分页列表（按商品维度）
    /// </summary>
    [HttpGet("reports")]
    public async Task<ApiResponseDto<PagedResponseDto<SampleReportDto>>> GetReports([FromQuery] SampleReportQueryDto query)
        => await _appService.GetReportListAsync(query);

    /// <summary>
    /// 获取样品/赠品按活动维度统计报表分页列表（P-SG-04）
    /// R5：数据源 InventoryLogs（SourceType 8=样品领用出库/9=赠品活动出库），按 ActivityId + ProductId 聚合
    /// </summary>
    [HttpGet("reports/by-activity")]
    public async Task<ApiResponseDto<PagedResponseDto<SampleActivityReportDto>>> GetReportsByActivity([FromQuery] SampleActivityReportQueryDto query)
        => await _appService.GetReportByActivityAsync(query);

    /// <summary>
    /// 获取样品/赠品按客户维度统计报表分页列表（P-SG-04）
    /// 数据源：SampleGiftReceive（样品领用），按 CustomerId 聚合
    /// </summary>
    [HttpGet("reports/by-customer")]
    public async Task<ApiResponseDto<PagedResponseDto<SampleCustomerReportDto>>> GetReportsByCustomer([FromQuery] SampleCustomerReportQueryDto query)
        => await _appService.GetReportByCustomerAsync(query);
}

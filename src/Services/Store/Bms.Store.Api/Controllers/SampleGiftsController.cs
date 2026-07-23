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
/// 提供档案管理 CRUD、库存查询、统计报表接口
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
    /// 获取样品/赠品详情
    /// </summary>
    [HttpGet("{id:long}")]
    public async Task<ApiResponseDto<ProductDto?>> GetById(long id)
        => await _appService.GetByIdAsync(id);

    /// <summary>
    /// 创建样品/赠品
    /// </summary>
    [HttpPost]
    public async Task<ApiResponseDto<ProductDto>> Create([FromBody] ProductCreateDto dto)
        => await _appService.CreateAsync(dto);

    /// <summary>
    /// 更新样品/赠品
    /// </summary>
    [HttpPut("{id:long}")]
    public async Task<ApiResponseDto<ProductDto>> Update(long id, [FromBody] ProductUpdateDto dto)
    {
        dto.Id = id;
        return await _appService.UpdateAsync(dto);
    }

    /// <summary>
    /// 删除样品/赠品
    /// </summary>
    [HttpDelete("{id:long}")]
    public async Task<ApiResponseDto> Delete(long id)
        => await _appService.DeleteAsync(id);

    /// <summary>
    /// 批量删除样品/赠品
    /// </summary>
    [HttpPost("batch")]
    public async Task<ApiResponseDto> BatchDelete([FromBody] BatchDeleteRequest request)
        => await _appService.BatchDeleteAsync(request.Ids);

    /// <summary>
    /// 获取样品/赠品库存查询分页列表
    /// </summary>
    [HttpGet("inventories")]
    public async Task<ApiResponseDto<PagedResponseDto<SampleInventoryDto>>> GetInventories([FromQuery] SampleInventoryQueryDto query)
        => await _appService.GetInventoryListAsync(query);

    /// <summary>
    /// 获取样品/赠品统计报表分页列表（按商品维度）
    /// </summary>
    [HttpGet("reports")]
    public async Task<ApiResponseDto<PagedResponseDto<SampleReportDto>>> GetReports([FromQuery] SampleReportQueryDto query)
        => await _appService.GetReportListAsync(query);

    /// <summary>
    /// 获取样品/赠品按活动维度统计报表分页列表（P-SG-04）
    /// 数据源：SampleGiftOut（赠品出库），按 ActivityId + ProductId 聚合
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

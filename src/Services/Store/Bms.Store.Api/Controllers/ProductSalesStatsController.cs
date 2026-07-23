using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Statistics;
using Bms.Store.Application.Services;

namespace Bms.Store.Api.Controllers;

/// <summary>
/// 商品销售统计管理控制器
/// </summary>
[ApiController]
[Route("api/store/[controller]")]
[Authorize]
public class ProductSalesStatsController : ControllerBase
{
    private readonly IProductSalesStatAppService _appService;

    public ProductSalesStatsController(IProductSalesStatAppService appService)
    {
        _appService = appService;
    }

    /// <summary>
    /// 获取商品销售统计分页列表
    /// </summary>
    [HttpGet]
    public async Task<ApiResponseDto<PagedResponseDto<ProductSalesStatDto>>> GetList([FromQuery] ProductSalesStatQueryDto query)
        => await _appService.GetPagedListAsync(query);

    /// <summary>
    /// 获取商品销售统计详情
    /// </summary>
    [HttpGet("{id:long}")]
    public async Task<ApiResponseDto<ProductSalesStatDto?>> GetById(long id)
        => await _appService.GetByIdAsync(id);

    /// <summary>
    /// 创建商品销售统计
    /// </summary>
    [HttpPost]
    public async Task<ApiResponseDto<ProductSalesStatDto>> Create([FromBody] ProductSalesStatCreateDto dto)
        => await _appService.CreateAsync(dto);

    /// <summary>
    /// 更新商品销售统计
    /// </summary>
    [HttpPut("{id:long}")]
    public async Task<ApiResponseDto<ProductSalesStatDto>> Update(long id, [FromBody] ProductSalesStatUpdateDto dto)
    {
        dto.Id = id;
        return await _appService.UpdateAsync(dto);
    }

    /// <summary>
    /// 删除商品销售统计
    /// </summary>
    [HttpDelete("{id:long}")]
    public async Task<ApiResponseDto> Delete(long id)
        => await _appService.DeleteAsync(id);

    /// <summary>
    /// 批量删除商品销售统计
    /// </summary>
    [HttpPost("batch")]
    public async Task<ApiResponseDto> BatchDelete([FromBody] BatchDeleteRequest request)
        => await _appService.BatchDeleteAsync(request.Ids);
}

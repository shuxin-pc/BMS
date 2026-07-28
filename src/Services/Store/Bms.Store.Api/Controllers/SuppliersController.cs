using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Suppliers;
using Bms.Store.Application.Services;

namespace Bms.Store.Api.Controllers;

/// <summary>
/// 供应商管理控制器
/// </summary>
[ApiController]
[Route("api/store/[controller]")]
[Authorize]
public class SuppliersController : ControllerBase
{
    private readonly ISupplierAppService _appService;

    public SuppliersController(ISupplierAppService appService)
    {
        _appService = appService;
    }

    /// <summary>
    /// 获取供应商分页列表
    /// </summary>
    [HttpGet]
    public async Task<ApiResponseDto<PagedResponseDto<SupplierDto>>> GetList([FromQuery] SupplierQueryDto query)
        => await _appService.GetPagedListAsync(query);

    /// <summary>
    /// 获取供应商详情
    /// </summary>
    [HttpGet("{id:long}")]
    public async Task<ApiResponseDto<SupplierDto?>> GetById(long id)
        => await _appService.GetByIdAsync(id);

    /// <summary>
    /// 创建供应商
    /// </summary>
    [HttpPost]
    public async Task<ApiResponseDto<SupplierDto>> Create([FromBody] SupplierCreateDto dto)
        => await _appService.CreateAsync(dto);

    /// <summary>
    /// 更新供应商
    /// </summary>
    [HttpPut("{id:long}")]
    public async Task<ApiResponseDto<SupplierDto>> Update(long id, [FromBody] SupplierUpdateDto dto)
    {
        dto.Id = id;
        return await _appService.UpdateAsync(dto);
    }

    /// <summary>
    /// 删除供应商
    /// </summary>
    [HttpDelete("{id:long}")]
    public async Task<ApiResponseDto> Delete(long id)
        => await _appService.DeleteAsync(id);

    /// <summary>
    /// 批量删除供应商
    /// </summary>
    [HttpPost("batch")]
    public async Task<ApiResponseDto> BatchDelete([FromBody] BatchDeleteRequest request)
        => await _appService.BatchDeleteAsync(request.Ids);

    // ========== 供应商-品项关联（G2.6.2）==========

    /// <summary>
    /// 批量绑定品项到供应商
    /// </summary>
    [HttpPost("bind-products")]
    public async Task<ApiResponseDto<List<ProductSupplierDto>>> BindProducts([FromBody] BindProductsDto dto)
        => await _appService.BindProductsAsync(dto);

    /// <summary>
    /// 解除品项与供应商的关联
    /// </summary>
    [HttpDelete("products/{productId:long}/{supplierId:long}")]
    public async Task<ApiResponseDto> UnbindProduct(long productId, long supplierId)
        => await _appService.UnbindProductAsync(productId, supplierId);

    /// <summary>
    /// 设置品项的默认供应商
    /// </summary>
    [HttpPost("default-supplier")]
    public async Task<ApiResponseDto<ProductSupplierDto>> SetDefaultSupplier([FromBody] SetDefaultSupplierDto dto)
        => await _appService.SetDefaultSupplierAsync(dto);

    /// <summary>
    /// 查询供应商关联的品项列表
    /// </summary>
    [HttpGet("{supplierId:long}/products")]
    public async Task<ApiResponseDto<List<ProductSupplierDto>>> GetProductsBySupplier(long supplierId)
        => await _appService.GetProductsBySupplierAsync(supplierId);

    /// <summary>
    /// 获取供应商轻量选项列表（不分页，用于下拉选择场景）
    /// </summary>
    [HttpGet("options")]
    public async Task<ApiResponseDto<List<SupplierOptionDto>>> GetOptions()
        => await _appService.GetOptionsAsync();
}

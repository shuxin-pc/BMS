using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Products;
using Bms.Store.Application.Dtos.Suppliers;
using Bms.Store.Application.Services;

namespace Bms.Store.Api.Controllers;

/// <summary>
/// 商品档案管理控制器
/// </summary>
[ApiController]
[Route("api/product/[controller]")]
[Authorize]
public class ProductsController : ControllerBase
{
    private readonly IProductAppService _productAppService;

    public ProductsController(IProductAppService productAppService)
    {
        _productAppService = productAppService;
    }

    /// <summary>
    /// 获取商品分页列表
    /// </summary>
    [HttpGet]
    public async Task<ApiResponseDto<PagedResponseDto<ProductDto>>> GetList([FromQuery] ProductQueryDto query)
    {
        return await _productAppService.GetPagedListAsync(query);
    }

    /// <summary>
    /// 获取商品详情
    /// </summary>
    [HttpGet("{id:long}")]
    public async Task<ApiResponseDto<ProductDto?>> GetById(long id)
    {
        return await _productAppService.GetByIdAsync(id);
    }

    /// <summary>
    /// 创建商品
    /// </summary>
    [HttpPost]
    public async Task<ApiResponseDto<ProductDto>> Create([FromBody] ProductCreateDto dto)
    {
        return await _productAppService.CreateAsync(dto);
    }

    /// <summary>
    /// 更新商品
    /// </summary>
    [HttpPut("{id:long}")]
    public async Task<ApiResponseDto<ProductDto>> Update(long id, [FromBody] ProductUpdateDto dto)
    {
        // 以 URL 中的 id 为准，防止 body 与路由不一致
        dto.Id = id;
        return await _productAppService.UpdateAsync(dto);
    }

    /// <summary>
    /// 删除商品
    /// </summary>
    [HttpDelete("{id:long}")]
    public async Task<ApiResponseDto> Delete(long id)
    {
        return await _productAppService.DeleteAsync(id);
    }

    /// <summary>
    /// 批量删除商品
    /// </summary>
    [HttpPost("batch")]
    public async Task<ApiResponseDto> BatchDelete([FromBody] BatchDeleteRequest request)
    {
        return await _productAppService.BatchDeleteAsync(request.Ids);
    }

    // ========== 品项-供应商关联（G2.6.2）==========

    /// <summary>
    /// 查询品项关联的供应商列表（默认供应商排在首位）
    /// </summary>
    [HttpGet("{productId:long}/suppliers")]
    public async Task<ApiResponseDto<List<ProductSupplierDto>>> GetSuppliersByProduct(long productId)
    {
        return await _productAppService.GetSuppliersByProductAsync(productId);
    }
}

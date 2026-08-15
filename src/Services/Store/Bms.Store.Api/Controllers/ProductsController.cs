using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Bms.BuildingBlocks.Abstractions.Security;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Products;
using Bms.Store.Application.Dtos.Suppliers;
using Bms.Store.Application.Services;

namespace Bms.Store.Api.Controllers;

/// <summary>
/// 门店商品档案管理控制器（门店隔离，承载分店差异化属性）
/// 对应设计文档 3.2 节：Master 字段只读，Store 字段可编辑
/// </summary>
[ApiController]
[Route("api/store/product/[controller]")]
[Authorize]
public class ProductsController : ControllerBase
{
    private readonly IProductAppService _productAppService;

    public ProductsController(IProductAppService productAppService)
    {
        _productAppService = productAppService;
    }

    /// <summary>
    /// 获取门店商品档案分页列表
    /// </summary>
    [Permission("store:product:profile:view")]
    [HttpGet]
    public async Task<ApiResponseDto<PagedResponseDto<ProductDto>>> GetList([FromQuery] ProductQueryDto query)
    {
        return await _productAppService.GetPagedListAsync(query);
    }

    /// <summary>
    /// 获取门店商品档案详情
    /// </summary>
    [Permission("store:product:profile:view")]
    [HttpGet("{id:long}")]
    public async Task<ApiResponseDto<ProductDto?>> GetById(long id)
    {
        return await _productAppService.GetByIdAsync(id);
    }

    /// <summary>
    /// 创建门店商品档案（需先存在 Master，按 MasterId 创建）
    /// </summary>
    [Permission("store:product:profile:edit")]
    [HttpPost]
    public async Task<ApiResponseDto<ProductDto>> Create([FromBody] ProductCreateDto dto)
    {
        return await _productAppService.CreateAsync(dto);
    }

    /// <summary>
    /// 更新门店商品档案（仅 Store 字段，Master 字段通过主档接口修改）
    /// </summary>
    [Permission("store:product:profile:edit")]
    [HttpPut("{id:long}")]
    public async Task<ApiResponseDto<ProductDto>> Update(long id, [FromBody] ProductUpdateDto dto)
    {
        // 以 URL 中的 id 为准，防止 body 与路由不一致
        dto.Id = id;
        return await _productAppService.UpdateAsync(dto);
    }

    /// <summary>
    /// 删除门店商品档案（库存为 0 才允许删除，对应设计文档 8.3 节）
    /// </summary>
    [Permission("store:product:profile:edit")]
    [HttpDelete("{id:long}")]
    public async Task<ApiResponseDto> Delete(long id)
    {
        return await _productAppService.DeleteAsync(id);
    }

    /// <summary>
    /// 批量删除门店商品档案（有库存的项自动跳过）
    /// </summary>
    [Permission("store:product:profile:edit")]
    [HttpPost("batch")]
    public async Task<ApiResponseDto> BatchDelete([FromBody] BatchDeleteRequest request)
    {
        return await _productAppService.BatchDeleteAsync(request.Ids);
    }

    // ========== 品项-供应商关联（G2.6.2）==========

    /// <summary>
    /// 查询品项关联的供应商列表（默认供应商排在首位）
    /// </summary>
    [Permission("store:product:profile:view")]
    [HttpGet("{productId:long}/suppliers")]
    public async Task<ApiResponseDto<List<ProductSupplierDto>>> GetSuppliersByProduct(long productId)
    {
        return await _productAppService.GetSuppliersByProductAsync(productId);
    }

    /// <summary>
    /// 获取商品轻量选项列表（不分页，用于下拉选择场景）
    /// </summary>
    [Permission("store:product:profile:view")]
    [HttpGet("options")]
    public async Task<ApiResponseDto<List<ProductOptionDto>>> GetOptions()
        => await _productAppService.GetOptionsAsync();
}

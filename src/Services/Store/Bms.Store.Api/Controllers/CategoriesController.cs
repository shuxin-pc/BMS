using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Products;
using Bms.Store.Application.Services;

namespace Bms.Store.Api.Controllers;

/// <summary>
/// 商品分类管理控制器
/// </summary>
[ApiController]
[Route("api/store/product/[controller]")]
[Authorize]
public class CategoriesController : ControllerBase
{
    private readonly IProductCategoryAppService _categoryAppService;

    public CategoriesController(IProductCategoryAppService categoryAppService)
    {
        _categoryAppService = categoryAppService;
    }

    /// <summary>
    /// 获取商品分类树
    /// </summary>
    [HttpGet("tree")]
    public async Task<ApiResponseDto<List<ProductCategoryDto>>> GetTree()
    {
        return await _categoryAppService.GetTreeAsync();
    }

    /// <summary>
    /// 创建商品分类
    /// </summary>
    [HttpPost]
    public async Task<ApiResponseDto<ProductCategoryDto>> Create([FromBody] ProductCategoryCreateDto dto)
    {
        return await _categoryAppService.CreateAsync(dto);
    }

    /// <summary>
    /// 更新商品分类
    /// </summary>
    [HttpPut("{id:long}")]
    public async Task<ApiResponseDto<ProductCategoryDto>> Update(long id, [FromBody] ProductCategoryUpdateDto dto)
    {
        // 以 URL 中的 id 为准，防止 body 与路由不一致
        dto.Id = id;
        return await _categoryAppService.UpdateAsync(dto);
    }

    /// <summary>
    /// 删除商品分类
    /// </summary>
    [HttpDelete("{id:long}")]
    public async Task<ApiResponseDto> Delete(long id)
    {
        return await _categoryAppService.DeleteAsync(id);
    }
}

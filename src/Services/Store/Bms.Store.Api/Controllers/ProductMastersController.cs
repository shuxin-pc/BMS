using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Bms.BuildingBlocks.Abstractions.Security;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Products;
using Bms.Store.Application.Services;

namespace Bms.Store.Api.Controllers;

/// <summary>
/// 商品主档管理控制器（租户级，承载商品本质属性）
/// 对应设计文档第二章/第五章/第七章
/// Master 字段全租户生效；Store 字段通过 BatchConfigStoreFields 统一配置
/// </summary>
[ApiController]
[Route("api/store/product/product-masters")]
[Authorize]
public class ProductMastersController : ControllerBase
{
    private readonly IProductMasterAppService _appService;

    public ProductMastersController(IProductMasterAppService appService)
    {
        _appService = appService;
    }

    /// <summary>
    /// 获取商品主档分页列表（租户级，含子表字段）
    /// </summary>
    [Permission("store:product:master:view")]
    [HttpGet]
    public async Task<ApiResponseDto<PagedResponseDto<ProductMasterDto>>> GetList([FromQuery] ProductMasterQueryDto query)
        => await _appService.GetPagedListAsync(query);

    /// <summary>
    /// 根据ID获取商品主档详情（含子表字段）
    /// </summary>
    [Permission("store:product:master:view")]
    [HttpGet("{id:long}")]
    public async Task<ApiResponseDto<ProductMasterDto?>> GetById(long id)
        => await _appService.GetByIdAsync(id);

    /// <summary>
    /// 创建商品主档（服务商品同时创建 ServiceProduct 子表）
    /// </summary>
    // [Permission("store:product:master:edit")]  // 临时注释：按钮权限待统一恢复
    [HttpPost]
    public async Task<ApiResponseDto<ProductMasterDto>> Create([FromBody] ProductMasterCreateDto dto)
        => await _appService.CreateAsync(dto);

    /// <summary>
    /// 更新商品主档（Master 字段全租户生效，含子表处理）
    /// </summary>
    // [Permission("store:product:master:edit")]  // 临时注释：按钮权限待统一恢复
    [HttpPut("{id:long}")]
    public async Task<ApiResponseDto<ProductMasterDto>> Update(long id, [FromBody] ProductMasterUpdateDto dto)
    {
        // 以 URL 中的 id 为准，防止 body 与路由不一致
        dto.Id = id;
        return await _appService.UpdateAsync(dto);
    }

    /// <summary>
    /// 删除商品主档（库存检查 + 级联软删除，对应设计文档 8.1 节）
    /// </summary>
    // [Permission("store:product:master:edit")]  // 临时注释：按钮权限待统一恢复
    [HttpDelete("{id:long}")]
    public async Task<ApiResponseDto> Delete(long id)
        => await _appService.DeleteAsync(id);

    /// <summary>
    /// 统一配置门店档案 Store 字段（对应设计文档 7.2/7.3 节）
    /// 将 Store 字段值应用到选中门店：已有 Product -> 覆盖；无 Product -> 自动创建
    /// </summary>
    // [Permission("store:product:master:edit")]  // 临时注释：按钮权限待统一恢复
    [HttpPost("{masterId:long}/store-config")]
    public async Task<ApiResponseDto> BatchConfigStoreFields(long masterId, [FromBody] ProductStoreBatchConfigDto dto)
    {
        // 以 URL 中的 masterId 为准，防止 body 与路由不一致
        dto.MasterId = masterId;
        return await _appService.BatchConfigStoreFieldsAsync(dto);
    }

    /// <summary>
    /// 预览门店档案配置（对应设计文档 7.3 节二次确认）
    /// 查询选中门店中哪些已存在档案（将被覆盖）、哪些无档案（将新建），用于前端二次确认动态提示
    /// </summary>
    [Permission("store:product:master:view")]
    [HttpGet("{masterId:long}/store-config/preview")]
    public async Task<ApiResponseDto<ProductStoreConfigPreviewDto>> GetStoreConfigPreview(long masterId, [FromQuery(Name = "storeIds")] string storeIds)
    {
        // storeIds 为逗号分隔的门店ID列表，转为 List<long>
        var storeIdList = storeIds?
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => long.TryParse(s.Trim(), out var id) ? id : 0)
            .Where(id => id > 0)
            .Distinct()
            .ToList() ?? new List<long>();

        return await _appService.GetStoreConfigPreviewAsync(masterId, storeIdList);
    }

    /// <summary>
    /// 获取商品主档轻量选项列表（下拉选择用）
    /// </summary>
    [Permission("store:product:master:view")]
    [HttpGet("options")]
    public async Task<ApiResponseDto<List<ProductMasterOptionDto>>> GetOptions()
        => await _appService.GetOptionsAsync();
}

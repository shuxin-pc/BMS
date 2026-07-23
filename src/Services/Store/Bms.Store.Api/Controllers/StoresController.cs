using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Stores;
using Bms.Store.Application.Services;

namespace Bms.Store.Api.Controllers;

/// <summary>
/// 门店档案管理控制器
/// </summary>
[ApiController]
[Route("api/store/[controller]")]
[Authorize]
public class StoresController : ControllerBase
{
    private readonly IStoreAppService _storeAppService;

    public StoresController(IStoreAppService storeAppService)
    {
        _storeAppService = storeAppService;
    }

    /// <summary>
    /// 获取门店分页列表
    /// </summary>
    [HttpGet]
    public async Task<ApiResponseDto<PagedResponseDto<StoreDto>>> GetList([FromQuery] StoreQueryDto query)
    {
        return await _storeAppService.GetPagedListAsync(query);
    }

    /// <summary>
    /// 获取当前用户授权的门店列表（用于门店切换器）
    /// </summary>
    [HttpGet("authorized")]
    public async Task<ApiResponseDto<List<StoreDto>>> GetAuthorizedStores()
    {
        return await _storeAppService.GetAuthorizedStoresAsync();
    }

    /// <summary>
    /// 获取门店详情
    /// </summary>
    [HttpGet("{id:long}")]
    public async Task<ApiResponseDto<StoreDto?>> GetById(long id)
    {
        return await _storeAppService.GetByIdAsync(id);
    }

    /// <summary>
    /// 创建门店
    /// </summary>
    [HttpPost]
    public async Task<ApiResponseDto<StoreDto>> Create([FromBody] StoreCreateDto dto)
    {
        return await _storeAppService.CreateAsync(dto);
    }

    /// <summary>
    /// 更新门店
    /// </summary>
    [HttpPut("{id:long}")]
    public async Task<ApiResponseDto<StoreDto>> Update(long id, [FromBody] StoreUpdateDto dto)
    {
        // 以 URL 中的 id 为准，防止 body 与路由不一致
        dto.Id = id;
        return await _storeAppService.UpdateAsync(dto);
    }

    /// <summary>
    /// 删除门店
    /// </summary>
    [HttpDelete("{id:long}")]
    public async Task<ApiResponseDto> Delete(long id)
    {
        return await _storeAppService.DeleteAsync(id);
    }

    /// <summary>
    /// 批量删除门店
    /// </summary>
    [HttpPost("batch")]
    public async Task<ApiResponseDto> BatchDelete([FromBody] BatchDeleteRequest request)
    {
        return await _storeAppService.BatchDeleteAsync(request.Ids);
    }
}

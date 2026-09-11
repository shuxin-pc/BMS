using Bms.BuildingBlocks.Core.Search;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bms.Store.Api.Controllers;

/// <summary>
/// 全局搜索控制器（门店业务数据）
/// </summary>
[ApiController]
[Route("api/store/search")]
[Authorize]
public class SearchController : ControllerBase
{
    private readonly IGlobalSearchAppService _appService;

    public SearchController(IGlobalSearchAppService appService)
    {
        _appService = appService;
    }

    /// <summary>
    /// 全局搜索（功能导航之外的门店业务数据：顾客/订单/商品/项目卡/储值/预约）
    /// </summary>
    /// <param name="request">搜索请求（关键字 + 每组条数上限）</param>
    [HttpGet]
    public async Task<ApiResponseDto<List<SearchResultGroupDto>>> Search([FromQuery] GlobalSearchRequestDto request)
        => await _appService.SearchAsync(request);
}

using Bms.BuildingBlocks.Core.Search;
using Bms.System.Application.Dtos;
using Bms.System.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bms.System.Api.Controllers;

/// <summary>
/// 全局搜索控制器（系统业务数据）
/// </summary>
[ApiController]
[Route("api/system/search")]
[Authorize]
public class SearchController : ControllerBase
{
    private readonly IGlobalSearchAppService _appService;

    public SearchController(IGlobalSearchAppService appService)
    {
        _appService = appService;
    }

    /// <summary>
    /// 全局搜索（系统业务数据：用户）
    /// </summary>
    /// <param name="request">搜索请求（关键字 + 每组条数上限）</param>
    [HttpGet]
    public async Task<ApiResponseDto<List<SearchResultGroupDto>>> Search([FromQuery] GlobalSearchRequestDto request)
        => await _appService.SearchAsync(request);
}

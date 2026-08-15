using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Customers;
using Bms.Store.Application.Services;

namespace Bms.Store.Api.Controllers;

/// <summary>
/// 客户标签管理控制器
/// </summary>
[ApiController]
[Route("api/store/[controller]")]
[Authorize]
public class CustomerTagsController : ControllerBase
{
    private readonly ICustomerTagAppService _appService;

    public CustomerTagsController(ICustomerTagAppService appService)
    {
        _appService = appService;
    }

    /// <summary>
    /// 获取客户标签分页列表
    /// </summary>
    [HttpGet]
    public async Task<ApiResponseDto<PagedResponseDto<CustomerTagDto>>> GetList([FromQuery] CustomerTagQueryDto query)
        => await _appService.GetPagedListAsync(query);

    /// <summary>
    /// 获取全量客户标签列表（供客户弹窗下拉使用，不分页）
    /// 注意：此路由需放在 {id:long} 之前，避免 "all" 被匹配为 id
    /// </summary>
    [HttpGet("all")]
    public async Task<ApiResponseDto<List<CustomerTagDto>>> GetAll()
        => await _appService.GetAllAsync();

    /// <summary>
    /// 获取客户标签详情
    /// </summary>
    [HttpGet("{id:long}")]
    public async Task<ApiResponseDto<CustomerTagDto?>> GetById(long id)
        => await _appService.GetByIdAsync(id);

    /// <summary>
    /// 创建客户标签
    /// </summary>
    [HttpPost]
    public async Task<ApiResponseDto<CustomerTagDto>> Create([FromBody] CustomerTagCreateDto dto)
        => await _appService.CreateAsync(dto);

    /// <summary>
    /// 更新客户标签
    /// </summary>
    [HttpPut("{id:long}")]
    public async Task<ApiResponseDto<CustomerTagDto>> Update(long id, [FromBody] CustomerTagUpdateDto dto)
    {
        dto.Id = id;
        return await _appService.UpdateAsync(dto);
    }

    /// <summary>
    /// 删除客户标签
    /// </summary>
    [HttpDelete("{id:long}")]
    public async Task<ApiResponseDto> Delete(long id)
        => await _appService.DeleteAsync(id);

    /// <summary>
    /// 批量删除客户标签
    /// </summary>
    [HttpPost("batch")]
    public async Task<ApiResponseDto> BatchDelete([FromBody] BatchDeleteRequest request)
        => await _appService.BatchDeleteAsync(request.Ids);
}

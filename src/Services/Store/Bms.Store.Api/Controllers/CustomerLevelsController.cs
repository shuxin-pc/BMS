using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Customers;
using Bms.Store.Application.Services;

namespace Bms.Store.Api.Controllers;

/// <summary>
/// 客户等级管理控制器
/// </summary>
[ApiController]
[Route("api/store/[controller]")]
[Authorize]
public class CustomerLevelsController : ControllerBase
{
    private readonly ICustomerLevelAppService _appService;

    public CustomerLevelsController(ICustomerLevelAppService appService)
    {
        _appService = appService;
    }

    /// <summary>
    /// 获取客户等级分页列表
    /// </summary>
    [HttpGet]
    public async Task<ApiResponseDto<PagedResponseDto<CustomerLevelDto>>> GetList([FromQuery] CustomerLevelQueryDto query)
        => await _appService.GetPagedListAsync(query);

    /// <summary>
    /// 获取客户等级详情
    /// </summary>
    [HttpGet("{id:long}")]
    public async Task<ApiResponseDto<CustomerLevelDto?>> GetById(long id)
        => await _appService.GetByIdAsync(id);

    /// <summary>
    /// 创建客户等级
    /// </summary>
    [HttpPost]
    public async Task<ApiResponseDto<CustomerLevelDto>> Create([FromBody] CustomerLevelCreateDto dto)
        => await _appService.CreateAsync(dto);

    /// <summary>
    /// 更新客户等级
    /// </summary>
    [HttpPut("{id:long}")]
    public async Task<ApiResponseDto<CustomerLevelDto>> Update(long id, [FromBody] CustomerLevelUpdateDto dto)
    {
        dto.Id = id;
        return await _appService.UpdateAsync(dto);
    }

    /// <summary>
    /// 删除客户等级
    /// </summary>
    [HttpDelete("{id:long}")]
    public async Task<ApiResponseDto> Delete(long id)
        => await _appService.DeleteAsync(id);

    /// <summary>
    /// 批量删除客户等级
    /// </summary>
    [HttpPost("batch")]
    public async Task<ApiResponseDto> BatchDelete([FromBody] BatchDeleteRequest request)
        => await _appService.BatchDeleteAsync(request.Ids);
}

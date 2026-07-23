using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Customers;
using Bms.Store.Application.Services;

namespace Bms.Store.Api.Controllers;

/// <summary>
/// 客户积分流水管理控制器
/// </summary>
[ApiController]
[Route("api/store/[controller]")]
[Authorize]
public class CustomerPointsLogsController : ControllerBase
{
    private readonly ICustomerPointsLogAppService _appService;

    public CustomerPointsLogsController(ICustomerPointsLogAppService appService)
    {
        _appService = appService;
    }

    /// <summary>
    /// 获取客户积分流水分页列表
    /// </summary>
    [HttpGet]
    public async Task<ApiResponseDto<PagedResponseDto<CustomerPointsLogDto>>> GetList([FromQuery] CustomerPointsLogQueryDto query)
        => await _appService.GetPagedListAsync(query);

    /// <summary>
    /// 获取客户积分流水详情
    /// </summary>
    [HttpGet("{id:long}")]
    public async Task<ApiResponseDto<CustomerPointsLogDto?>> GetById(long id)
        => await _appService.GetByIdAsync(id);

    /// <summary>
    /// 创建客户积分流水
    /// </summary>
    [HttpPost]
    public async Task<ApiResponseDto<CustomerPointsLogDto>> Create([FromBody] CustomerPointsLogCreateDto dto)
        => await _appService.CreateAsync(dto);

    /// <summary>
    /// 更新客户积分流水
    /// </summary>
    [HttpPut("{id:long}")]
    public async Task<ApiResponseDto<CustomerPointsLogDto>> Update(long id, [FromBody] CustomerPointsLogUpdateDto dto)
    {
        dto.Id = id;
        return await _appService.UpdateAsync(dto);
    }

    /// <summary>
    /// 删除客户积分流水
    /// </summary>
    [HttpDelete("{id:long}")]
    public async Task<ApiResponseDto> Delete(long id)
        => await _appService.DeleteAsync(id);

    /// <summary>
    /// 批量删除客户积分流水
    /// </summary>
    [HttpPost("batch")]
    public async Task<ApiResponseDto> BatchDelete([FromBody] BatchDeleteRequest request)
        => await _appService.BatchDeleteAsync(request.Ids);
}

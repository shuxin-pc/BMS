using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Customers;
using Bms.Store.Application.Services;

namespace Bms.Store.Api.Controllers;

/// <summary>
/// 客户档案管理控制器
/// </summary>
[ApiController]
[Route("api/store/[controller]")]
[Authorize]
public class CustomersController : ControllerBase
{
    private readonly ICustomerAppService _appService;

    public CustomersController(ICustomerAppService appService)
    {
        _appService = appService;
    }

    /// <summary>
    /// 获取客户分页列表
    /// </summary>
    [HttpGet]
    public async Task<ApiResponseDto<PagedResponseDto<CustomerDto>>> GetList([FromQuery] CustomerQueryDto query)
        => await _appService.GetPagedListAsync(query);

    /// <summary>
    /// 获取客户详情
    /// </summary>
    [HttpGet("{id:long}")]
    public async Task<ApiResponseDto<CustomerDto?>> GetById(long id)
        => await _appService.GetByIdAsync(id);

    /// <summary>
    /// 创建客户
    /// </summary>
    [HttpPost]
    public async Task<ApiResponseDto<CustomerDto>> Create([FromBody] CustomerCreateDto dto)
        => await _appService.CreateAsync(dto);

    /// <summary>
    /// 更新客户
    /// </summary>
    [HttpPut("{id:long}")]
    public async Task<ApiResponseDto<CustomerDto>> Update(long id, [FromBody] CustomerUpdateDto dto)
    {
        dto.Id = id;
        return await _appService.UpdateAsync(dto);
    }

    /// <summary>
    /// 删除客户
    /// </summary>
    [HttpDelete("{id:long}")]
    public async Task<ApiResponseDto> Delete(long id)
        => await _appService.DeleteAsync(id);

    /// <summary>
    /// 永久删除客户档案（物理删除）
    /// 物理删除客户档案及关联个人信息，订单脱敏保留
    /// 前置条件：无未完成订单、无未核销疗程卡、无储值余额
    /// 需二次确认（客户手机号后4位）
    /// 依据：《个人信息保护法》第 47 条
    /// </summary>
    [HttpDelete("{id:long}/permanent")]
    public async Task<ApiResponseDto> PermanentlyDelete(long id, [FromBody] CustomerPermanentDeleteDto dto)
        => await _appService.PermanentlyDeleteAsync(id, dto);

    /// <summary>
    /// 批量删除客户
    /// </summary>
    [HttpPost("batch")]
    public async Task<ApiResponseDto> BatchDelete([FromBody] BatchDeleteRequest request)
        => await _appService.BatchDeleteAsync(request.Ids);

    /// <summary>
    /// 获取客户消费统计（消费频次、客单价、消费偏好）
    /// </summary>
    [HttpGet("{id:long}/consumption-stat")]
    public async Task<ApiResponseDto<CustomerConsumptionStatDto>> GetConsumptionStat(long id)
        => await _appService.GetConsumptionStatAsync(id);
}

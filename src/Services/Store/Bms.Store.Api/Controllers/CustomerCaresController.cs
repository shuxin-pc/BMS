using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Customers;
using Bms.Store.Application.Services;

namespace Bms.Store.Api.Controllers;

/// <summary>
/// 客户关怀管理控制器
/// 生日提醒和消费感谢的查询与标记
/// </summary>
[ApiController]
[Route("api/store/[controller]")]
[Authorize]
public class CustomerCaresController : ControllerBase
{
    private readonly ICustomerCareAppService _appService;

    public CustomerCaresController(ICustomerCareAppService appService)
    {
        _appService = appService;
    }

    /// <summary>
    /// 获取生日提醒分页列表
    /// </summary>
    [HttpGet("birthdays")]
    public async Task<ApiResponseDto<PagedResponseDto<BirthdayReminderDto>>> GetBirthdays([FromQuery] BirthdayReminderQueryDto query)
        => await _appService.GetBirthdayRemindersAsync(query);

    /// <summary>
    /// 标记生日关怀完成
    /// </summary>
    /// <param name="customerId">客户ID</param>
    [HttpPost("birthdays/{customerId:long}/mark")]
    public async Task<ApiResponseDto> MarkBirthdayCared(long customerId)
        => await _appService.MarkBirthdayCaredAsync(customerId);

    /// <summary>
    /// 获取消费感谢分页列表
    /// </summary>
    [HttpGet("consumeThanks")]
    public async Task<ApiResponseDto<PagedResponseDto<ConsumeThankRecordDto>>> GetConsumeThanks([FromQuery] ConsumeThankQueryDto query)
        => await _appService.GetConsumeThanksAsync(query);

    /// <summary>
    /// 标记消费感谢完成
    /// </summary>
    /// <param name="orderId">订单ID</param>
    [HttpPost("consumeThanks/{orderId:long}/mark")]
    public async Task<ApiResponseDto> MarkConsumeThanked(long orderId, [FromBody] ConsumeThankMarkRequest request)
        => await _appService.MarkConsumeThankedAsync(orderId, request.Method);
}

/// <summary>
/// 消费感谢标记请求
/// </summary>
public class ConsumeThankMarkRequest
{
    /// <summary>
    /// 感谢方式 1=短信 2=微信 3=电话
    /// </summary>
    public int Method { get; set; }
}

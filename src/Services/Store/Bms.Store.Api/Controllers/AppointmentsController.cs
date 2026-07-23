using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Appointments;
using Bms.Store.Application.Services;

namespace Bms.Store.Api.Controllers;

/// <summary>
/// 预约管理控制器
/// </summary>
[ApiController]
[Route("api/store/[controller]")]
[Authorize]
public class AppointmentsController : ControllerBase
{
    private readonly IAppointmentAppService _appService;

    public AppointmentsController(IAppointmentAppService appService)
    {
        _appService = appService;
    }

    /// <summary>
    /// 获取预约分页列表
    /// </summary>
    [HttpGet]
    public async Task<ApiResponseDto<PagedResponseDto<AppointmentDto>>> GetList([FromQuery] AppointmentQueryDto query)
        => await _appService.GetPagedListAsync(query);

    /// <summary>
    /// 获取预约详情
    /// </summary>
    [HttpGet("{id:long}")]
    public async Task<ApiResponseDto<AppointmentDto?>> GetById(long id)
        => await _appService.GetByIdAsync(id);

    /// <summary>
    /// 创建预约
    /// </summary>
    [HttpPost]
    public async Task<ApiResponseDto<AppointmentDto>> Create([FromBody] AppointmentCreateDto dto)
        => await _appService.CreateAsync(dto);

    /// <summary>
    /// 更新预约
    /// </summary>
    [HttpPut("{id:long}")]
    public async Task<ApiResponseDto<AppointmentDto>> Update(long id, [FromBody] AppointmentUpdateDto dto)
    {
        dto.Id = id;
        return await _appService.UpdateAsync(dto);
    }

    /// <summary>
    /// 删除预约
    /// </summary>
    [HttpDelete("{id:long}")]
    public async Task<ApiResponseDto> Delete(long id)
        => await _appService.DeleteAsync(id);

    /// <summary>
    /// 批量删除预约
    /// </summary>
    [HttpPost("batch")]
    public async Task<ApiResponseDto> BatchDelete([FromBody] BatchDeleteRequest request)
        => await _appService.BatchDeleteAsync(request.Ids);

    /// <summary>
    /// 获取明日预约提醒分页列表
    /// </summary>
    [HttpGet("tomorrowReminders")]
    public async Task<ApiResponseDto<PagedResponseDto<TomorrowReminderDto>>> GetTomorrowReminders([FromQuery] TomorrowReminderQueryDto query)
        => await _appService.GetTomorrowRemindersAsync(query);

    /// <summary>
    /// 发送预约提醒（更新提醒状态为已提醒）
    /// </summary>
    [HttpPost("{id:long}/reminder")]
    public async Task<ApiResponseDto> SendReminder(long id)
        => await _appService.SendReminderAsync(id);

    /// <summary>
    /// 确认明日预约（更新状态为已确认）
    /// </summary>
    [HttpPut("{id:long}/confirm")]
    public async Task<ApiResponseDto> ConfirmAppointment(long id)
        => await _appService.ConfirmTomorrowAppointmentAsync(id);
}

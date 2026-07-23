using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Appointments;

namespace Bms.Store.Application.Services;

/// <summary>
/// 预约应用服务接口
/// </summary>
public interface IAppointmentAppService
{
    Task<ApiResponseDto<PagedResponseDto<AppointmentDto>>> GetPagedListAsync(AppointmentQueryDto query);
    Task<ApiResponseDto<AppointmentDto?>> GetByIdAsync(long id);
    Task<ApiResponseDto<AppointmentDto>> CreateAsync(AppointmentCreateDto dto);
    Task<ApiResponseDto<AppointmentDto>> UpdateAsync(AppointmentUpdateDto dto);
    Task<ApiResponseDto> DeleteAsync(long id);
    Task<ApiResponseDto> BatchDeleteAsync(List<long> ids);

    /// <summary>
    /// 获取明日预约提醒分页列表（AppointmentDate=明天 AND Status IN(1,2)）
    /// </summary>
    Task<ApiResponseDto<PagedResponseDto<TomorrowReminderDto>>> GetTomorrowRemindersAsync(TomorrowReminderQueryDto query);

    /// <summary>
    /// 发送提醒（更新提醒状态为已提醒，记录提醒时间）
    /// </summary>
    Task<ApiResponseDto> SendReminderAsync(long id);

    /// <summary>
    /// 确认明日预约（更新状态为已确认，记录确认时间）
    /// </summary>
    Task<ApiResponseDto> ConfirmTomorrowAppointmentAsync(long id);
}

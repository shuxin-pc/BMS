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
}

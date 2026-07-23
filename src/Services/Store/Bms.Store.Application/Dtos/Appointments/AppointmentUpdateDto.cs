namespace Bms.Store.Application.Dtos.Appointments;

/// <summary>
/// 更新预约输入 DTO
/// </summary>
public class AppointmentUpdateDto : AppointmentCreateDto
{
    public long Id { get; set; }
}

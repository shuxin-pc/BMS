namespace Bms.Store.Application.Dtos.Technicians;

/// <summary>
/// 更新商家技师请求 DTO
/// </summary>
public class TechnicianUpdateDto : TechnicianCreateDto
{
    /// <summary>
    /// 技师ID
    /// </summary>
    public long Id { get; set; }
}

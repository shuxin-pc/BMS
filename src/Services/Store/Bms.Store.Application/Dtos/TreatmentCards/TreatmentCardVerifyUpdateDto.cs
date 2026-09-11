namespace Bms.Store.Application.Dtos.TreatmentCards;

/// <summary>
/// 更新项目卡核销记录请求 DTO
/// </summary>
public class TreatmentCardVerifyUpdateDto : TreatmentCardVerifyCreateDto
{
    /// <summary>
    /// 核销记录ID
    /// </summary>
    public long Id { get; set; }
}

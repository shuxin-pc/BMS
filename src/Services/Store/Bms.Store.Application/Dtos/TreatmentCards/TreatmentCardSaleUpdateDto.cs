namespace Bms.Store.Application.Dtos.TreatmentCards;

/// <summary>
/// 更新项目卡销售记录请求 DTO
/// </summary>
public class TreatmentCardSaleUpdateDto : TreatmentCardSaleCreateDto
{
    /// <summary>
    /// 销售记录ID
    /// </summary>
    public long Id { get; set; }
}

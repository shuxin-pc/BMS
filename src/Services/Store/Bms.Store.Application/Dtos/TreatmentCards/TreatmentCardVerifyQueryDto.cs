using Bms.Store.Application.Dtos;

namespace Bms.Store.Application.Dtos.TreatmentCards;

/// <summary>
/// 疗程卡核销记录分页查询 DTO
/// </summary>
public class TreatmentCardVerifyQueryDto : PagedRequestDto
{
    /// <summary>
    /// 疗程卡销售记录ID
    /// </summary>
    public long? CardSaleId { get; set; }

    /// <summary>
    /// 核销项目ID
    /// </summary>
    public long? VerifyProductId { get; set; }
}

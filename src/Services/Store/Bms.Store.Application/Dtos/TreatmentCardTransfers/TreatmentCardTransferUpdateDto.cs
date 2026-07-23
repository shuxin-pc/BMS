namespace Bms.Store.Application.Dtos.TreatmentCardTransfers;

/// <summary>
/// 更新疗程卡转让请求 DTO
/// </summary>
public class TreatmentCardTransferUpdateDto : TreatmentCardTransferCreateDto
{
    /// <summary>
    /// 转让记录ID
    /// </summary>
    public long Id { get; set; }
}

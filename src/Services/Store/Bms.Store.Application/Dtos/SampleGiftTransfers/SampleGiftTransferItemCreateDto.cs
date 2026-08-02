namespace Bms.Store.Application.Dtos.SampleGiftTransfers;

/// <summary>
/// 创建样品赠品调拨单明细输入 DTO
/// 级联创建时 SampleGiftTransferId 由后端按主单回填，前端无需传入
/// </summary>
public class SampleGiftTransferItemCreateDto
{
    /// <summary>
    /// 调拨单ID（级联创建时由后端回填，独立创建明细时由前端传入）
    /// </summary>
    public long SampleGiftTransferId { get; set; }

    public long ProductId { get; set; }
    public decimal Quantity { get; set; }
    public string? BatchNo { get; set; }
    public string? Remark { get; set; }
}

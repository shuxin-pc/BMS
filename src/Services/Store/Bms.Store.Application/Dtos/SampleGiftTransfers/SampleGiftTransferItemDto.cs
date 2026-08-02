namespace Bms.Store.Application.Dtos.SampleGiftTransfers;

/// <summary>
/// 样品赠品调拨单明细输出 DTO
/// </summary>
public class SampleGiftTransferItemDto
{
    public long Id { get; set; }
    public long SampleGiftTransferId { get; set; }
    public long ProductId { get; set; }
    public string? ProductName { get; set; }
    public string? ProductCode { get; set; }
    public string? Unit { get; set; }
    public decimal Quantity { get; set; }
    public string? BatchNo { get; set; }
    public string? Remark { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

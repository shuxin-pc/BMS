namespace Bms.Store.Application.Dtos.SampleGifts;

/// <summary>
/// 赠品出库记录输出 DTO
/// </summary>
public class SampleGiftOutDto
{
    public long Id { get; set; }
    public long ProductId { get; set; }
    public long InventoryBatchId { get; set; }
    public decimal Quantity { get; set; }
    public DateTime OutTime { get; set; }
    public long? ActivityId { get; set; }
    public long? OrderId { get; set; }
    public long? OperatorId { get; set; }
    public string? Remark { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

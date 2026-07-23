namespace Bms.Store.Application.Dtos.SampleGifts;

/// <summary>
/// 创建赠品出库记录输入 DTO
/// </summary>
public class SampleGiftOutCreateDto
{
    public long ProductId { get; set; }
    public long InventoryBatchId { get; set; }
    public decimal Quantity { get; set; }
    public DateTime OutTime { get; set; }
    public long? ActivityId { get; set; }
    public long? OrderId { get; set; }
    public long? OperatorId { get; set; }
    public string? Remark { get; set; }
}

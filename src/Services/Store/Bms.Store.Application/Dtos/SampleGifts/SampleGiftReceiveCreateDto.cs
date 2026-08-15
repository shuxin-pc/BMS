namespace Bms.Store.Application.Dtos.SampleGifts;

/// <summary>
/// 创建样品领用记录输入 DTO
/// </summary>
public class SampleGiftReceiveCreateDto
{
    public long ProductId { get; set; }
    public long InventoryBatchId { get; set; }
    /// <summary>
    /// 客户ID（可选，未选择客户时为 null）
    /// </summary>
    public long? CustomerId { get; set; }
    public decimal Quantity { get; set; }
    public DateTime ReceiveTime { get; set; }
    public long? OperatorId { get; set; }
    public string? Remark { get; set; }

    /// <summary>
    /// 关联活动ID（可选，用于活动维度归因统计）
    /// </summary>
    public long? ActivityId { get; set; }
}

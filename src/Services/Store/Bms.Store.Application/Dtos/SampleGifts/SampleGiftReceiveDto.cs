namespace Bms.Store.Application.Dtos.SampleGifts;

/// <summary>
/// 样品领用记录输出 DTO
/// </summary>
public class SampleGiftReceiveDto
{
    public long Id { get; set; }
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
    /// 关联活动ID（可选）
    /// </summary>
    public long? ActivityId { get; set; }

    /// <summary>
    /// 关联活动名称（显示字段，由 AppService Include 填充）
    /// </summary>
    public string? ActivityName { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

namespace Bms.Store.Application.Dtos.SampleGiftTransfers;

/// <summary>
/// 样品赠品调拨单输出 DTO
/// </summary>
public class SampleGiftTransferDto
{
    public long Id { get; set; }
    public string TransferNo { get; set; } = string.Empty;
    public long FromStoreId { get; set; }
    public string? FromStoreCode { get; set; }
    public string? FromStoreName { get; set; }
    public long ToStoreId { get; set; }
    public string? ToStoreCode { get; set; }
    public string? ToStoreName { get; set; }
    public DateTime TransferDate { get; set; }
    public int Status { get; set; }
    public long? OperatorId { get; set; }
    public string? OperatorName { get; set; }
    public string? Remark { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// 调拨明细列表
    /// </summary>
    public List<SampleGiftTransferItemDto> Items { get; set; } = new();
}

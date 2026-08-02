namespace Bms.Store.Application.Dtos.SampleGiftTransfers;

/// <summary>
/// 创建样品赠品调拨单输入 DTO
/// TransferNo 由后端自动生成，传入值会被忽略
/// </summary>
public class SampleGiftTransferCreateDto
{
    public long FromStoreId { get; set; }
    public string? FromStoreCode { get; set; }
    public long ToStoreId { get; set; }
    public string? ToStoreCode { get; set; }
    public DateTime TransferDate { get; set; }

    /// <summary>
    /// 初始状态固定为待调出（1），由 CreateAsync 强制设置，传入值会被忽略
    /// </summary>
    public int Status { get; set; } = 1;

    public long? OperatorId { get; set; }
    public string? Remark { get; set; }

    /// <summary>
    /// 调拨明细列表（级联创建，CreateAsync 中一并写入）
    /// </summary>
    public List<SampleGiftTransferItemCreateDto> Items { get; set; } = new();
}

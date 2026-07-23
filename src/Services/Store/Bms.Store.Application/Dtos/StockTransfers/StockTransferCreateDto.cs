namespace Bms.Store.Application.Dtos.StockTransfers;

/// <summary>
/// 创建库存调拨单输入 DTO
/// </summary>
public class StockTransferCreateDto
{
    public string TransferNo { get; set; } = string.Empty;
    public long FromStoreId { get; set; }
    public string? FromStoreCode { get; set; }
    public long ToStoreId { get; set; }
    public string? ToStoreCode { get; set; }
    public DateTime TransferDate { get; set; }

    /// <summary>
    /// 初始状态固定为待调出（草稿），由 CreateAsync 强制设置，传入值会被忽略
    /// </summary>
    public int Status { get; set; } = 1;

    public long? OperatorId { get; set; }
    public string? Remark { get; set; }
}

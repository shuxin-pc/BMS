namespace Bms.Store.Application.Dtos.StockTransfers;

/// <summary>
/// 库存调拨单输出 DTO
/// </summary>
public class StockTransferDto
{
    public long Id { get; set; }
    public string TransferNo { get; set; } = string.Empty;
    public long FromStoreId { get; set; }
    public string? FromStoreCode { get; set; }
    public long ToStoreId { get; set; }
    public string? ToStoreCode { get; set; }
    public DateTime TransferDate { get; set; }
    public int Status { get; set; }
    public long? OperatorId { get; set; }
    public string? Remark { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

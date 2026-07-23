namespace Bms.Store.Application.Dtos.StockTransfers;

/// <summary>
/// 更新库存调拨单明细输入 DTO
/// </summary>
public class StockTransferItemUpdateDto : StockTransferItemCreateDto
{
    public long Id { get; set; }
}

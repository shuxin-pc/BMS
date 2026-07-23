namespace Bms.Store.Application.Dtos.StockTransfers;

/// <summary>
/// 更新库存调拨单输入 DTO
/// </summary>
public class StockTransferUpdateDto : StockTransferCreateDto
{
    public long Id { get; set; }
}

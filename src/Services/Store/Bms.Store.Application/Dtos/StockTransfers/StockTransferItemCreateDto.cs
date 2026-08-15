namespace Bms.Store.Application.Dtos.StockTransfers;

/// <summary>
/// 创建库存调拨单明细输入 DTO
/// 级联创建时 StockTransferId 由后端按主单回填，前端无需传入
/// </summary>
public class StockTransferItemCreateDto
{
    /// <summary>
    /// 调拨单ID（级联创建时由后端回填，独立创建明细时由前端传入）
    /// </summary>
    public long StockTransferId { get; set; }

    public long ProductId { get; set; }
    public decimal Quantity { get; set; }
    public string? BatchNo { get; set; }
    public string? Remark { get; set; }
}

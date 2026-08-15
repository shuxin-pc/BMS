namespace Bms.Store.Application.Dtos.StockTransfers;

/// <summary>
/// 调拨专用商品选项（含调出门店库存量，用于新增调拨时下拉选择）
/// 仅返回调出门店有库存的商品，避免选到无库存商品
/// </summary>
public class StockTransferProductOptionDto
{
    public long Id { get; set; }

    /// <summary>
    /// 商品名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 商品编码
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 商品类型（1:实物商品 2:服务商品 3:耗材 4:样品 5:赠品）
    /// </summary>
    public int Type { get; set; }

    /// <summary>
    /// 单位
    /// </summary>
    public string? Unit { get; set; }

    /// <summary>
    /// 调出门店该商品库存量
    /// </summary>
    public decimal Stock { get; set; }
}

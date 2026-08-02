namespace Bms.Store.Application.Dtos.SampleGiftTransfers;

/// <summary>
/// 样品赠品调拨专用商品选项（含调出门店库存量，用于新增调拨时下拉选择）
/// 仅返回调出门店有库存的样品(4)/赠品(5)商品
/// </summary>
public class SampleGiftTransferProductOptionDto
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
    /// 单位
    /// </summary>
    public string? Unit { get; set; }

    /// <summary>
    /// 调出门店该商品库存量
    /// </summary>
    public decimal Stock { get; set; }
}

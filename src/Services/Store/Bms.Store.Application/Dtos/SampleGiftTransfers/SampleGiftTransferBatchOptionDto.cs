namespace Bms.Store.Application.Dtos.SampleGiftTransfers;

/// <summary>
/// 样品赠品调拨专用批次选项（用于手动指定批次模式下的批次下拉选择）
/// 仅返回调出门店该商品在库且有库存的批次，按过期日期升序（FEFO）
/// </summary>
public class SampleGiftTransferBatchOptionDto
{
    public long Id { get; set; }

    /// <summary>
    /// 批次号
    /// </summary>
    public string BatchNo { get; set; } = string.Empty;

    /// <summary>
    /// 当前批次库存数量
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// 采购单价（用于成本追溯）
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// 过期日期
    /// </summary>
    public DateTime? ExpirationDate { get; set; }

    /// <summary>
    /// 生产日期
    /// </summary>
    public DateTime? ProductionDate { get; set; }
}

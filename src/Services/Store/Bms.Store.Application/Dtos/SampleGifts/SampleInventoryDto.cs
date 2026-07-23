namespace Bms.Store.Application.Dtos.SampleGifts;

/// <summary>
/// 样品库存信息
/// </summary>
public class SampleInventoryDto
{
    /// <summary>
    /// 档案ID
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 编码
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 类型（4:样品 5:赠品）
    /// </summary>
    public int Type { get; set; }

    /// <summary>
    /// 单位
    /// </summary>
    public string? Unit { get; set; }

    /// <summary>
    /// 当前库存
    /// </summary>
    public decimal CurrentStock { get; set; }

    /// <summary>
    /// 预警阈值（未配置时为 null，表示不参与低库存预警）
    /// </summary>
    public decimal? AlertQuantity { get; set; }

    /// <summary>
    /// 库存状态（1:充足 2:偏低 3:不足）
    /// </summary>
    public int InventoryStatus { get; set; }

    /// <summary>
    /// 上次入库时间
    /// </summary>
    public DateTime? LastInboundTime { get; set; }
}

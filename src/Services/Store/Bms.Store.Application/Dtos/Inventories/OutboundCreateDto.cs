namespace Bms.Store.Application.Dtos.Inventories;

/// <summary>
/// 出库请求 DTO（对应 POST /api/store/InventoryLogs/outbound）
/// 支持两种模式互斥：Quantity（FEFO 自动）或 BatchItems（手动指定批次）
/// </summary>
public class OutboundCreateDto
{
    /// <summary>商品ID</summary>
    public long ProductId { get; set; }

    /// <summary>
    /// 出库来源类型，取值见 InventoryLogSourceTypes：
    /// 3=盘点盘亏 5=调拨出库 6=其他 7=采购退货出库 9=样品赠品(历史) 10=样品领用 11=赠品活动
    /// 禁止手动创建的来源 {0=销售出库, 8=疗程卡核销出库} 由 Validator 拒绝
    /// </summary>
    public int SourceType { get; set; }

    /// <summary>
    /// FEFO 自动模式：总出库量（>0），后端按 FEFO 顺序自动分配到各批次。
    /// 与 BatchItems 互斥：只能填一个。
    /// </summary>
    public decimal? Quantity { get; set; }

    /// <summary>
    /// 手动模式：批次扣减明细。提供时按用户指定批次扣减，不走 FEFO。
    /// 与 Quantity 互斥：只能填一个。
    /// </summary>
    public List<BatchOutboundItem>? BatchItems { get; set; }

    /// <summary>
    /// 单价（仅记录到流水，不覆盖批次原值）。
    /// 手动模式下若填则所有批次流水统一用此值，不填取各批次原值。
    /// </summary>
    public decimal? UnitPrice { get; set; }

    /// <summary>备注</summary>
    public string? Remark { get; set; }
}
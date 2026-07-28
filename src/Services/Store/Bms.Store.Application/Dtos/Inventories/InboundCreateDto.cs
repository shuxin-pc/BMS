namespace Bms.Store.Application.Dtos.Inventories;

/// <summary>
/// 入库请求 DTO（对应 POST /api/store/InventoryLogs/inbound）
/// </summary>
public class InboundCreateDto
{
    /// <summary>商品ID</summary>
    public long ProductId { get; set; }

    /// <summary>
    /// 入库来源类型，取值见 InventoryLogSourceTypes：
    /// 1=采购入库 2=退货入库 3=盘点调整 4=调拨入库 6=其他
    /// 非入库来源 {0,5,7,8,9,10,11} 由 Validator 拒绝
    /// </summary>
    public int SourceType { get; set; }

    /// <summary>供应商ID（SourceType=1 采购入库时必填）</summary>
    public long? SupplierId { get; set; }

    /// <summary>入库数量（>0）</summary>
    public decimal Quantity { get; set; }

    /// <summary>单价（用于 FIFO 成本计算）</summary>
    public decimal? UnitPrice { get; set; }

    /// <summary>批次号（仅用于内部追溯，不参与扣减排序）</summary>
    public string? BatchNo { get; set; }

    /// <summary>生产日期（仅写入 InventoryBatch）</summary>
    public DateTime? ProductionDate { get; set; }

    /// <summary>保质期天数（仅写入 InventoryBatch）</summary>
    public int? ShelfLifeDays { get; set; }

    /// <summary>过期日期；未填视为"无效期限制"批次</summary>
    public DateTime? ExpirationDate { get; set; }

    /// <summary>备注（仅写入 InventoryLog）</summary>
    public string? Remark { get; set; }
}

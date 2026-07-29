namespace Bms.Store.Application.Dtos.Inventories;

/// <summary>
/// 出库结果汇总 DTO（对应 POST /api/store/InventoryLogs/outbound 响应）
/// 包含总出库量与批次扣减明细列表
/// </summary>
public class OutboundResultDto
{
    /// <summary>商品ID</summary>
    public long ProductId { get; set; }

    /// <summary>商品名称（显示字段）</summary>
    public string? ProductName { get; set; }

    /// <summary>商品编码（显示字段）</summary>
    public string? ProductCode { get; set; }

    /// <summary>出库来源类型</summary>
    public int SourceType { get; set; }

    /// <summary>总出库数量（正数）</summary>
    public decimal TotalQuantity { get; set; }

    /// <summary>操作前库存</summary>
    public decimal BeforeQuantity { get; set; }

    /// <summary>操作后库存</summary>
    public decimal AfterQuantity { get; set; }

    /// <summary>操作人</summary>
    public string? OperatorName { get; set; }

    /// <summary>批次扣减明细列表（每条对应一个批次的扣减流水）</summary>
    public List<InventoryLogDto> BatchDetails { get; set; } = new();
}
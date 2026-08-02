namespace Bms.Store.Domain.Entities;

/// <summary>
/// 样品赠品调拨单
/// 记录样品(4)/赠品(5)的跨门店调拨，状态机：待调出(1) -> 已调入(3) / 已取消(4)
/// 库存处理与 StockTransfer 一致，独立成表避免与正品调拨查询互相污染
/// 软删除：无（调拨单属非核心表单据，与 StockTransfer 一致）
/// </summary>
public class SampleGiftTransfer : StoreBusinessEntityBase
{
    /// <summary>调拨单号 SGT{yyyyMMdd}{序号}</summary>
    public string TransferNo { get; set; } = string.Empty;

    /// <summary>调出门店ID</summary>
    public long FromStoreId { get; set; }

    /// <summary>调出门店编码（冗余存储，便于展示）</summary>
    public string? FromStoreCode { get; set; }

    /// <summary>调出门店名称（冗余存储，便于展示）</summary>
    public string? FromStoreName { get; set; }

    /// <summary>调入门店ID</summary>
    public long ToStoreId { get; set; }

    /// <summary>调入门店编码（冗余存储，便于展示）</summary>
    public string? ToStoreCode { get; set; }

    /// <summary>调入门店名称（冗余存储，便于展示）</summary>
    public string? ToStoreName { get; set; }

    /// <summary>调拨日期</summary>
    public DateTime TransferDate { get; set; }

    /// <summary>
    /// 状态（1:待调出 3:已调入 4:已取消，跳过 2 与正品调拨状态值保持兼容）
    /// 流转：1 -> 3（ExecuteAsync） / 1 -> 4（CancelAsync）
    /// 3 与 4 为终态；如需撤销已完成调拨，应创建反向调拨单
    /// </summary>
    public int Status { get; set; } = 1;

    /// <summary>操作员ID</summary>
    public long? OperatorId { get; set; }

    /// <summary>操作员姓名（冗余存储，便于展示）</summary>
    public string? OperatorName { get; set; }

    /// <summary>备注</summary>
    public string? Remark { get; set; }

    /// <summary>调拨明细列表</summary>
    public List<SampleGiftTransferItem> Items { get; set; } = new();
}

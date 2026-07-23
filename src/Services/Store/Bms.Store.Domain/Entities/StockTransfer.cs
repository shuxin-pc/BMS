namespace Bms.Store.Domain.Entities;

/// <summary>
/// 库存调拨单
/// 记录门店间库存调拨，状态机闭环：待调出(草稿) -> 已调入(已完成) / 已取消
/// </summary>
public class StockTransfer : StoreBusinessEntityBase
{
    /// <summary>
    /// 调拨单号
    /// </summary>
    public string TransferNo { get; set; } = string.Empty;

    /// <summary>
    /// 调出门店ID
    /// </summary>
    public long FromStoreId { get; set; }

    /// <summary>
    /// 调出门店编码
    /// </summary>
    public string? FromStoreCode { get; set; }

    /// <summary>
    /// 调入门店ID
    /// </summary>
    public long ToStoreId { get; set; }

    /// <summary>
    /// 调入门店编码
    /// </summary>
    public string? ToStoreCode { get; set; }

    /// <summary>
    /// 调拨日期
    /// </summary>
    public DateTime TransferDate { get; set; }

    /// <summary>
    /// 状态（1:待调出/草稿 2:已调出 3:已调入/已完成 4:已取消）
    /// 状态流转：1 -> 3（ExecuteAsync） / 1 -> 4（CancelAsync）
    /// 3 与 4 为终态；如需撤销已完成调拨，应创建反向调拨单
    /// </summary>
    public int Status { get; set; } = 1;

    /// <summary>
    /// 操作员ID
    /// </summary>
    public long? OperatorId { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}

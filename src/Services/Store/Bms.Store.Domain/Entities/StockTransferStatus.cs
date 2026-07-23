namespace Bms.Store.Domain.Entities;

/// <summary>
/// 库存调拨单状态常量
/// 状态值与 StockTransfer.Status 字段保持一致，集中管理避免魔法数字
/// 状态机闭环：待调出(草稿) -> 已调入(已完成) / 已取消
/// </summary>
public static class StockTransferStatus
{
    /// <summary>待调出（草稿：已创建未执行，可修改/执行/取消）</summary>
    public const int Draft = 1;

    /// <summary>已调出（中间态，当前流程不使用，保留兼容历史数据）</summary>
    public const int Outbound = 2;

    /// <summary>已调入（已完成：库存已联动扣减与增加，终态）</summary>
    public const int Completed = 3;

    /// <summary>已取消（终态，不调整库存；如需撤销已完成调拨，应创建反向调拨单）</summary>
    public const int Cancelled = 4;

    /// <summary>
    /// 判定状态值是否为合法的调拨单状态
    /// </summary>
    /// <param name="status">状态值</param>
    /// <returns>合法返回 true，否则 false</returns>
    public static bool IsValid(int status) => status >= Draft && status <= Cancelled;
}

namespace Bms.Store.Domain.Entities;

/// <summary>
/// 库存盘点单状态常量
/// 状态值与 InventoryCheck.Status 字段保持一致，集中管理避免魔法数字
/// </summary>
public static class InventoryCheckStatus
{
    /// <summary>草稿（创建后初始状态，可修改/提交/取消）</summary>
    public const int Draft = 0;

    /// <summary>已完成（提交后终态，库存已联动调整）</summary>
    public const int Completed = 1;

    /// <summary>已取消（草稿取消，终态，不调整库存）</summary>
    public const int Cancelled = 2;

    /// <summary>
    /// 判定状态值是否为合法的盘点单状态
    /// </summary>
    /// <param name="status">状态值</param>
    /// <returns>合法返回 true，否则 false</returns>
    public static bool IsValid(int status) => status >= Draft && status <= Cancelled;
}

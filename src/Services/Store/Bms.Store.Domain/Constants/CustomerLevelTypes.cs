namespace Bms.Store.Domain.Constants;

/// <summary>
/// 客户等级类型常量
/// 系统仅允许创建普通会员(Level=1)和会员(Level=2)两个等级
/// 依据：G5.2 客户等级仅支持普通/会员两级
/// </summary>
public static class CustomerLevelTypes
{
    /// <summary>
    /// 普通会员（默认等级）
    /// </summary>
    public const int Normal = 1;

    /// <summary>
    /// 会员（升级等级）
    /// </summary>
    public const int Member = 2;

    /// <summary>
    /// 校验等级值是否合法
    /// </summary>
    /// <param name="level">等级值</param>
    /// <returns>合法返回 true，否则 false</returns>
    public static bool IsValid(int level) => level == Normal || level == Member;
}

namespace Bms.Store.Domain.Constants;

/// <summary>
/// 客户等级类型常量
/// 仅用于新租户首次 Seed 默认等级，不对门店可创建的等级数量或等级值做限制
/// </summary>
public static class CustomerLevelTypes
{
    /// <summary>
    /// 普通会员（默认 Seed 等级）
    /// </summary>
    public const int Normal = 1;

    /// <summary>
    /// 会员（默认 Seed 等级）
    /// </summary>
    public const int Member = 2;
}

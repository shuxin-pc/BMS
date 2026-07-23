namespace Bms.Store.Domain.Constants;

/// <summary>
/// 客户积分流水类型常量
/// 统一定义积分变动的类型枚举，避免散落在各服务中的魔法数字
/// 依据：G5.3 积分体系
/// </summary>
public static class CustomerPointsLogType
{
    /// <summary>
    /// 消费获得（订单消费按 PointsRate 计算发放）
    /// </summary>
    public const int Consume = 1;

    /// <summary>
    /// 兑换消耗（积分兑换商品/服务时扣减）
    /// </summary>
    public const int Exchange = 2;

    /// <summary>
    /// 退款扣减（订单退款时按比例扣减已发积分）
    /// </summary>
    public const int RefundDeduct = 3;

    /// <summary>
    /// 活动赠送（营销活动赠送积分）
    /// </summary>
    public const int ActivityGift = 4;

    /// <summary>
    /// 充值获得（储值充值按 PointsRate 计算发放）
    /// </summary>
    public const int Recharge = 5;

    /// <summary>
    /// 疗程卡购买获得（疗程卡购买时一次性发放，核销不再重复发放）
    /// </summary>
    public const int TreatmentCardPurchase = 6;

    /// <summary>
    /// 过期清零（按 PointsRule.PointsValidityDays 自动清零，系统操作）
    /// </summary>
    public const int Expire = 7;

    /// <summary>
    /// 手动调整（后台手动调整积分，必须填写原因备注）
    /// </summary>
    public const int ManualAdjust = 8;

    /// <summary>
    /// 校验类型值是否合法
    /// </summary>
    /// <param name="type">类型值</param>
    /// <returns>合法返回 true，否则 false</returns>
    public static bool IsValid(int type) => type >= Consume && type <= ManualAdjust;
}

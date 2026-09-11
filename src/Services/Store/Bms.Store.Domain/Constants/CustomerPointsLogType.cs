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
    /// 积分抵扣（订单支付时用积分抵扣金额，由 OrderAppService.DeductPointsAsync 写入）
    /// 复用已移除的 Exchange=2 位置，当前仅开发环境无历史数据需要区分
    /// </summary>
    public const int PointsDeduct = 2;

    /// <summary>
    /// 退款扣减（订单退款时按比例扣减已发积分）
    /// </summary>
    public const int RefundDeduct = 3;

    /// <summary>
    /// 充值获得（储值充值按 PointsRate 计算发放）
    /// </summary>
    public const int Recharge = 5;

    /// <summary>
    /// 项目卡购买获得（项目卡购买时一次性发放，核销不再重复发放）
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
    /// 退款退还（订单退款时退还积分抵扣部分的积分，由 OrderAppService.RefundPointsPaymentAsync 写入）
    /// </summary>
    public const int RefundReturn = 9;

    /// <summary>
    /// 校验类型值是否合法
    /// </summary>
    /// <param name="type">类型值</param>
    /// <returns>合法返回 true，否则 false</returns>
    public static bool IsValid(int type) => ValidTypes.Contains(type);

    /// <summary>
    /// 合法类型集合（4 为已移除的 ActivityGift 空洞，用集合校验避免误判）
    /// </summary>
    private static readonly HashSet<int> ValidTypes = new()
    {
        Consume, PointsDeduct, RefundDeduct, Recharge, TreatmentCardPurchase, Expire, ManualAdjust, RefundReturn
    };
}

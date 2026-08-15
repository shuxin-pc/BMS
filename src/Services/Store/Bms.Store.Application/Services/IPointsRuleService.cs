using Bms.Store.Domain.Entities;

namespace Bms.Store.Application.Services;

/// <summary>
/// 积分规则领域服务：统一封装生效规则查询与积分计算逻辑
/// 供 OrderAppService、TreatmentCardSaleAppService、StoredValueAccountAppService 等积分发放场景共用
/// </summary>
public interface IPointsRuleService
{
    /// <summary>
    /// 查询生效的积分规则：按当前门店隔离（StoreId 精确匹配），Status=1 且未软删除，不回退租户级
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    /// <param name="storeId">门店ID</param>
    /// <returns>生效的积分规则；门店未配置则返回 null</returns>
    Task<PointsRule?> GetEffectivePointsRuleAsync(long tenantId, long storeId);

    /// <summary>
    /// 计算应发积分：按 PointsRate 计算，Floor 取整
    /// - 校验 PointsRate > 0
    /// - 校验 baseAmount > 0
    /// - baseAmount 低于 MinAmountThreshold 门槛不发积分（消费、储值充值、疗程卡购买三个场景统一适用）
    /// - BirthdayDouble=true 且客户生日当天（月+日相等）-> 积分 ×2
    /// </summary>
    /// <param name="rule">生效的积分规则</param>
    /// <param name="customerBirthday">客户生日（null 表示未设置，不享受生日双倍）</param>
    /// <param name="baseAmount">发放基数（消费金额/充值金额/疗程卡售价）</param>
    /// <param name="now">当前时间（用于生日当天判断）</param>
    /// <returns>应发积分；不满足发放条件返回 0</returns>
    int CalculateAwardPoints(PointsRule? rule, DateTime? customerBirthday, decimal baseAmount, DateTime now);
}

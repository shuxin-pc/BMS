namespace Bms.Store.Application.Services;

/// <summary>
/// 积分抵扣领域服务接口
/// 统一封装积分抵扣逻辑（生效规则查询、单笔上限校验、向上取整换算、余额校验、扣减并记录兑换与流水）
/// 供订单组合支付（OrderAppService）、项目卡开卡组合支付（TreatmentCardSaleAppService）等场景共用，
/// 消除各服务中重复的私有积分扣减实现
/// </summary>
public interface IPointsDeductionService
{
    /// <summary>
    /// 按抵扣金额扣减客户积分（在调用方开启的事务内执行；不更新累计消费，由调用方按各自口径处理）
    /// 成功后写入 PointsExchange（ExchangeType=3 服务项目积分消费）与 CustomerPointsLog（PointsDeduct 积分抵扣）
    /// </summary>
    /// <param name="customerId">客户ID</param>
    /// <param name="tenantId">租户ID</param>
    /// <param name="tenantCode">租户编码</param>
    /// <param name="storeId">门店ID</param>
    /// <param name="storeCode">门店编码</param>
    /// <param name="amount">积分抵扣金额（>0）</param>
    /// <param name="refId">来源单据ID（订单ID或开卡销售ID），写入积分流水关联字段</param>
    /// <param name="refNo">来源单据号（订单号或开卡销售号），用于记录文案</param>
    /// <param name="refTypeName">来源单据类型名（如"订单"/"项目卡"），用于记录文案</param>
    /// <param name="operatorId">业务操作员ID（写入兑换记录 OperatorId）</param>
    /// <param name="now">当前时间</param>
    /// <returns>实际扣减的积分数量（向上取整换算）</returns>
    /// <exception cref="InvalidOperationException">规则未配置/抵扣金额超过单笔上限/客户不存在/积分不足时抛出</exception>
    Task<int> DeductAsync(
        long customerId,
        long tenantId, string tenantCode,
        long storeId, string storeCode,
        decimal amount,
        long? refId, string refNo, string refTypeName,
        long? operatorId,
        DateTime now);
}

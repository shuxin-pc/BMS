namespace Bms.Store.Application.Services;

/// <summary>
/// 储值赠送规则领域服务：统一封装充值赠送金额的计算逻辑
/// 供 StoredValueAccountAppService（实际充值）与 StoredValueRulesController（前端试算）共用，
/// 保证试算结果与入账结果始终一致
/// </summary>
public interface IStoredValueGiftRuleService
{
    /// <summary>
    /// 按"档位整倍叠加"口径计算赠送金额
    /// 规则：取当天生效且启用的档位，按充值金额从大到小贪心拆分，每个档位可命中多次，余额不足一档的部分不赠送。
    /// 例：档位 [充1000送110、充500送50]，充1700 = 1×1000档 + 1×500档 → 赠送160，余200不赠送
    /// 注意：贪心策略在"小档赠送比例更高"的反常配置下不保证赠送最大化
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    /// <param name="amount">客户实际充值金额</param>
    /// <param name="now">当前时间（用于判断档位是否在有效期内）</param>
    /// <returns>赠送金额；无命中档位返回 0</returns>
    Task<decimal> CalculateGiftAmountAsync(long tenantId, decimal amount, DateTime now);
}

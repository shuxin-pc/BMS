using Microsoft.EntityFrameworkCore;
using Bms.Store.Infrastructure;

namespace Bms.Store.Application.Services;

/// <summary>
/// 储值赠送规则领域服务实现
/// </summary>
public class StoredValueGiftRuleService : IStoredValueGiftRuleService
{
    private readonly StoreDbContext _dbContext;

    public StoredValueGiftRuleService(StoreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// 按"档位整倍叠加"口径计算赠送金额
    /// </summary>
    public async Task<decimal> CalculateGiftAmountAsync(long tenantId, decimal amount, DateTime now)
    {
        if (amount <= 0)
            return 0m;

        var today = now.Date;

        // 储值档位为租户级共享配置（与规则列表、原充值逻辑的过滤口径保持一致，不按门店隔离）
        // Amount > 0 是拆分计算的前提，验证器已拦截，此处再过滤一次避免历史脏数据导致死数据参与计算
        var rules = await _dbContext.StoredValueRules
            .Where(r => !r.IsDeleted
                        && r.TenantId == tenantId
                        && r.IsEnabled
                        && r.Amount > 0
                        && r.StartDate.Date <= today
                        && (r.EndDate == null || r.EndDate.Value.Date >= today))
            .OrderByDescending(r => r.Amount)
            // 同额档位并存属于配置错误，取赠送高的一条保证结果可预期
            .ThenByDescending(r => r.GiftAmount)
            .ToListAsync();

        var giftAmount = 0m;
        var remainAmount = amount;

        foreach (var rule in rules)
        {
            if (remainAmount < rule.Amount)
                continue;

            var times = Math.Floor(remainAmount / rule.Amount);
            giftAmount += times * rule.GiftAmount;
            remainAmount -= times * rule.Amount;
        }

        return giftAmount;
    }
}

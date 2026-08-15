using Microsoft.EntityFrameworkCore;
using Bms.Store.Domain.Entities;
using Bms.Store.Infrastructure;

namespace Bms.Store.Application.Services;

/// <summary>
/// 积分规则领域服务实现
/// 统一封装生效规则查询（门店隔离 + Status 过滤）与积分计算（含生日当天双倍）
/// 替代 OrderAppService、TreatmentCardSaleAppService 中重复的私有 GetEffectivePointsRuleAsync
/// </summary>
public class PointsRuleService : IPointsRuleService
{
    private readonly StoreDbContext _dbContext;

    public PointsRuleService(StoreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// 查询生效的积分规则：按当前门店隔离（StoreId 精确匹配），Status=1 且未软删除，不回退租户级
    /// </summary>
    public async Task<PointsRule?> GetEffectivePointsRuleAsync(long tenantId, long storeId)
    {
        return await _dbContext.PointsRules
            .FirstOrDefaultAsync(r => !r.IsDeleted && r.TenantId == tenantId && r.StoreId == storeId && r.Status == 1);
    }

    /// <summary>
    /// 计算应发积分：按 PointsRate 计算，Floor 取整
    /// - 校验 PointsRate > 0
    /// - 校验 baseAmount > 0
    /// - baseAmount 低于 MinAmountThreshold 门槛不发积分（消费、储值充值、疗程卡购买三个场景统一适用）
    /// - BirthdayDouble=true 且客户生日当天（月+日相等）-> 积分 ×2
    /// </summary>
    public int CalculateAwardPoints(PointsRule? rule, DateTime? customerBirthday, decimal baseAmount, DateTime now)
    {
        if (rule == null || rule.PointsRate <= 0 || baseAmount <= 0) return 0;

        // 最低获取门槛：发放基数未达门槛不发积分（门槛为 0 或 null 视为无门槛）
        if (rule.MinAmountThreshold.HasValue && baseAmount < rule.MinAmountThreshold.Value) return 0;

        var points = (int)Math.Floor(baseAmount * rule.PointsRate);
        if (points <= 0) return 0;

        // 生日当天双倍积分（月+日相等）
        if (rule.BirthdayDouble && customerBirthday.HasValue
            && customerBirthday.Value.Month == now.Month
            && customerBirthday.Value.Day == now.Day)
        {
            points *= 2;
        }

        return points;
    }
}

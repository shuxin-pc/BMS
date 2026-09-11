using Bms.BuildingBlocks.Abstractions.Security;
using Bms.Store.Domain.Constants;
using Bms.Store.Domain.Entities;
using Bms.Store.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Bms.Store.Application.Services;

/// <summary>
/// 积分抵扣领域服务实现
/// 统一封装积分抵扣逻辑：获取生效规则 → 单笔上限校验 → 向上取整换算扣减积分 → 余额校验 → 扣减
/// 并记录 PointsExchange（兑换记录）与 CustomerPointsLog（积分流水），供订单/开卡组合支付共用
/// </summary>
public class PointsDeductionService : IPointsDeductionService
{
    private readonly StoreDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IPointsRuleService _pointsRuleService;

    public PointsDeductionService(StoreDbContext dbContext, ICurrentUser currentUser, IPointsRuleService pointsRuleService)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _pointsRuleService = pointsRuleService;
    }

    public async Task<int> DeductAsync(
        long customerId,
        long tenantId, string tenantCode,
        long storeId, string storeCode,
        decimal amount,
        long? refId, string refNo, string refTypeName,
        long? operatorId,
        DateTime now)
    {
        if (amount <= 0)
            throw new InvalidOperationException("积分抵扣金额必须大于0");

        var rule = await _pointsRuleService.GetEffectivePointsRuleAsync(tenantId, storeId);
        if (rule == null || rule.DeductRate <= 0)
            throw new InvalidOperationException("未配置启用的积分规则或抵扣比例为0");

        // 校验抵扣金额不超过单笔上限
        if (rule.MaxDeductAmount > 0 && amount > rule.MaxDeductAmount)
            throw new InvalidOperationException($"积分抵扣金额超过单笔上限（{rule.MaxDeductAmount:F2}）");

        // 计算需扣减的积分（向上取整，避免少扣）
        var pointsToDeduct = (int)Math.Ceiling(amount / rule.DeductRate);

        var customer = await _dbContext.Customers
            .FirstOrDefaultAsync(c => c.Id == customerId && c.TenantId == tenantId);
        if (customer == null)
            throw new InvalidOperationException("客户不存在");

        if (customer.TotalPoints < pointsToDeduct)
            throw new InvalidOperationException($"客户积分不足（当前 {customer.TotalPoints}，需要 {pointsToDeduct}）");

        var beforePoints = customer.TotalPoints;
        customer.TotalPoints -= pointsToDeduct;
        customer.UpdatedTime = now;

        _dbContext.PointsExchanges.Add(new PointsExchange
        {
            CustomerId = customer.Id,
            ExchangeType = 3, // 服务项目积分消费
            TargetId = null,
            TargetName = $"{refTypeName}积分抵扣-{refNo}",
            PointsCost = pointsToDeduct,
            Quantity = 1,
            ExchangeTime = now,
            OperatorId = operatorId,
            Remark = $"{refTypeName} {refNo} 积分抵扣 {amount:F2}元",
            TenantId = tenantId,
            TenantCode = tenantCode,
            CreatedTime = now
        });

        // 积分抵扣支付属于积分使用场景，必须记录积分流水
        _dbContext.CustomerPointsLogs.Add(new CustomerPointsLog
        {
            CustomerId = customer.Id,
            Type = CustomerPointsLogType.PointsDeduct, // 积分抵扣
            Points = -pointsToDeduct,
            BeforePoints = beforePoints,
            AfterPoints = customer.TotalPoints,
            OrderId = refId,
            OperatorId = _currentUser.UserId,
            Remark = $"{refTypeName} {refNo} 积分抵扣 {amount:F2}元",
            TenantId = tenantId,
            TenantCode = tenantCode,
            StoreId = storeId,
            StoreCode = storeCode,
            CreatedTime = now
        });

        return pointsToDeduct;
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Bms.BuildingBlocks.Core.Context;
using Bms.Store.Domain.Constants;
using Bms.Store.Domain.Entities;
using Bms.Store.Infrastructure;

namespace Bms.Store.Api.BackgroundServices;

/// <summary>
/// 积分过期清零定时任务（P-PTS-04 第一阶段）
/// 每日凌晨 01:00 扫描已过期但未清零的积分记录，按客户聚合后清零并写入 Type=7 过期清零流水
/// 第二阶段（即将到期提醒）待站内信模块完善后单独实现
/// </summary>
public class PointsExpiryService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PointsExpiryService> _logger;

    /// <summary>
    /// 每日执行时刻（凌晨 01:00，避开 02:00 的日结任务和 03:00 的库存预警任务）
    /// </summary>
    private static readonly TimeSpan ExecuteTime = new(1, 0, 0);

    public PointsExpiryService(
        IServiceProvider serviceProvider,
        ILogger<PointsExpiryService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// 定时执行积分过期清零任务
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("积分过期清零任务已启动，执行时刻：每日 {Time}", ExecuteTime);

        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = GetDelayToNextRun();
            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            try
            {
                await ProcessExpiredPointsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "积分过期清零任务执行异常");
            }
        }
    }

    /// <summary>
    /// 计算到下次执行时刻的延迟时间
    /// </summary>
    private TimeSpan GetDelayToNextRun()
    {
        var now = DateTime.Now;
        var nextRun = now.Date.Add(ExecuteTime);
        if (nextRun <= now)
            nextRun = nextRun.AddDays(1);
        return nextRun - now;
    }

    /// <summary>
    /// 执行积分过期清零
    /// 流程：
    /// 1. 查询已过期但未清零的发放类积分记录（ExpireDate &lt; today, Points &gt; 0, IsExpired=false）
    /// 2. 按客户分组聚合应清零的积分
    /// 3. 扣减客户 TotalPoints（不允许负数，最低扣至 0）
    /// 4. 标记原记录 IsExpired=true
    /// 5. 写入 Type=7 过期清零流水（OperatorId=0 表示系统操作）
    /// </summary>
    private async Task ProcessExpiredPointsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<StoreDbContext>();

        // 审计日志豁免：后台定时任务无用户操作语义，关闭审计避免脏日志
        scope.ServiceProvider.GetRequiredService<IAuditLogContext>().IsEnabled = false;

        var today = DateTime.Today;
        var now = DateTime.Now;

        // 查询已过期但未清零的发放类积分记录
        var expiredLogs = await dbContext.CustomerPointsLogs
            .Where(l => l.ExpireDate.HasValue
                && l.ExpireDate < today
                && l.Points > 0
                && !l.IsExpired)
            .ToListAsync(cancellationToken);

        if (!expiredLogs.Any())
        {
            _logger.LogInformation("积分过期清零：无过期积分需要处理");
            return;
        }

        // 按客户分组聚合应清零的积分
        var byCustomer = expiredLogs.GroupBy(l => new { l.CustomerId, l.TenantId, l.TenantCode });
        var totalProcessed = 0;
        var totalCustomers = 0;

        foreach (var group in byCustomer)
        {
            var customerId = group.Key.CustomerId;
            var tenantId = group.Key.TenantId;
            var tenantCode = group.Key.TenantCode;
            var pointsToDeduct = group.Sum(l => l.Points);
            var logCount = group.Count();

            var customer = await dbContext.Customers
                .FirstOrDefaultAsync(c => c.Id == customerId && c.TenantId == tenantId, cancellationToken);
            if (customer == null)
            {
                _logger.LogWarning("积分过期清零：客户 ID={CustomerId} 不存在，跳过", customerId);
                continue;
            }

            // 不允许 TotalPoints 变为负数：实际扣减 = Min(应扣减, 当前积分)
            var beforePoints = customer.TotalPoints;
            var actualDeduct = Math.Min(pointsToDeduct, customer.TotalPoints);
            customer.TotalPoints -= actualDeduct;
            customer.UpdatedTime = now;

            // 标记原记录已清零（即使实际扣减 < 应扣减，也标记为已清零，避免重复扫描）
            foreach (var log in group)
            {
                log.IsExpired = true;
                log.UpdatedTime = now;
            }

            // 写入过期清零记录（OperatorId=0 表示系统操作）
            dbContext.CustomerPointsLogs.Add(new CustomerPointsLog
            {
                CustomerId = customerId,
                Type = CustomerPointsLogType.Expire, // 过期清零
                Points = -actualDeduct,
                BeforePoints = beforePoints,
                AfterPoints = customer.TotalPoints,
                OrderId = null,
                OperatorId = 0, // 系统操作
                ExpireDate = null,
                Remark = $"积分过期清零（共 {logCount} 笔记录，应扣 {pointsToDeduct}，实扣 {actualDeduct}）",
                TenantId = tenantId,
                TenantCode = tenantCode,
                StoreId = 0,
                StoreCode = string.Empty,
                CreatedTime = now
            });

            totalProcessed += logCount;
            totalCustomers++;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation(
            "积分过期清零完成：处理 {CustomerCount} 个客户、{LogCount} 笔过期记录",
            totalCustomers, totalProcessed);
    }
}

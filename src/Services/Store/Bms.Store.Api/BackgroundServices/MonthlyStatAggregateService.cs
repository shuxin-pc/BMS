using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Bms.Store.Application.Services;
using Bms.Store.Infrastructure;

namespace Bms.Store.Api.BackgroundServices;

/// <summary>
/// 月度统计自动聚合定时任务（P-STAT-01）
/// 每月 1 日凌晨 02:30 触发，遍历所有门店聚合上一个月的 DailyStat 生成 MonthlyStat
/// 也补全当月数据（如果当月已有 DailyStat），确保月度统计及时生成
/// </summary>
public class MonthlyStatAggregateService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MonthlyStatAggregateService> _logger;

    /// <summary>
    /// 每月执行时刻（每月 1 日凌晨 02:30）
    /// </summary>
    private static readonly TimeSpan ExecuteTime = new(2, 30, 0);

    public MonthlyStatAggregateService(
        IServiceProvider serviceProvider,
        ILogger<MonthlyStatAggregateService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// 定时执行月度聚合任务
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("月度统计自动聚合任务已启动，执行时刻：每月 1 日 {Time}", ExecuteTime);

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
                await AggregateAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "月度统计自动聚合任务执行异常");
            }
        }
    }

    /// <summary>
    /// 计算到下次执行时刻（每月 1 日 02:30）的延迟时间
    /// </summary>
    private TimeSpan GetDelayToNextRun()
    {
        var now = DateTime.Now;
        var nextRun = new DateTime(now.Year, now.Month, 1, ExecuteTime.Hours, ExecuteTime.Minutes, ExecuteTime.Seconds);
        if (now.Day == 1 && now.TimeOfDay < ExecuteTime)
        {
            // 当月 1 日但还未到执行时刻，本月执行
        }
        else
        {
            // 否则下月 1 日执行
            nextRun = nextRun.AddMonths(1);
        }
        return nextRun - now;
    }

    /// <summary>
    /// 遍历所有门店，聚合上月和当月的月度统计数据
    /// </summary>
    private async Task AggregateAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<StoreDbContext>();
        var monthlyStatService = scope.ServiceProvider.GetRequiredService<IMonthlyStatAppService>();

        // 查询所有未删除门店
        var stores = await dbContext.Stores
            .Where(s => !s.IsDeleted)
            .Select(s => new { s.Id, s.Code, s.TenantId, s.TenantCode })
            .ToListAsync(cancellationToken);

        if (stores.Count == 0)
        {
            _logger.LogInformation("月度统计自动聚合：未发现门店，跳过");
            return;
        }

        // 聚合上月数据（月度结束后次月 1 日生成完整数据）
        var today = DateTime.Today;
        var lastMonth = today.AddMonths(-1);
        var currentMonth = today;

        var success = 0;
        var failed = 0;

        foreach (var store in stores)
        {
            // 聚合上月
            var lastMonthResult = await monthlyStatService.AggregateFromDailyInternalAsync(
                store.TenantId, store.Id, store.TenantCode ?? string.Empty,
                lastMonth.Year, lastMonth.Month, cancellationToken);

            if (lastMonthResult != null)
                success++;
            else
                failed++;

            // 聚合当月（保证当月已日结的数据及时汇总到月度）
            var currentMonthResult = await monthlyStatService.AggregateFromDailyInternalAsync(
                store.TenantId, store.Id, store.TenantCode ?? string.Empty,
                currentMonth.Year, currentMonth.Month, cancellationToken);

            if (currentMonthResult != null)
                success++;
            else
                failed++;
        }

        _logger.LogInformation(
            "月度统计自动聚合完成：共处理 {Stores} 家门店 × 2 个月份，成功 {Success}，失败 {Failed}",
            stores.Count, success, failed);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Bms.BuildingBlocks.Core.Context;
using Bms.Store.Domain.Entities;
using Bms.Store.Infrastructure;

namespace Bms.Store.Api.BackgroundServices;

/// <summary>
/// 预约爽约自动判断定时任务
/// 扫描超过预约时段且状态为"已预约"(1)的预约,自动改为"爽约"(5)
/// </summary>
public class AppointmentNoShowService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AppointmentNoShowService> _logger;

    /// <summary>
    /// 扫描间隔(30分钟)
    /// 选用 30 分钟而非更短间隔，平衡爽约标记及时性与数据库压力
    /// </summary>
    private static readonly TimeSpan ScanInterval = TimeSpan.FromMinutes(30);

    /// <summary>
    /// 扫描窗口(前 7 天)
    /// 仅扫描近 7 天的已预约预约，避免全表扫描历史数据
    /// </summary>
    private static readonly int ScanWindowDays = 7;

    /// <summary>
    /// 单批处理最大数量
    /// 多租户场景下分批提交，避免单次 SaveChanges 数据量过大导致超时
    /// </summary>
    private const int BatchSize = 500;

    public AppointmentNoShowService(
        IServiceProvider serviceProvider,
        ILogger<AppointmentNoShowService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// 定时执行扫描任务
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("预约爽约自动判断定时任务已启动,扫描间隔:{Interval}分钟", ScanInterval.TotalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ScanAndMarkNoShowAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "预约爽约自动判断任务执行异常");
            }

            await Task.Delay(ScanInterval, stoppingToken);
        }
    }

    /// <summary>
    /// 扫描过期未到店的预约并标记为爽约
    /// 条件:状态为已预约(1) 且 预约日期在近 7 天内 且 预约结束时间已过
    /// 结束时间优先取 EndTime(由 AppointmentAppService 根据 ServiceProduct.Duration 自动计算)
    /// </summary>
    private async Task ScanAndMarkNoShowAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<StoreDbContext>();

        // 审计日志豁免：后台定时任务无用户操作语义，关闭审计避免脏日志
        scope.ServiceProvider.GetRequiredService<IAuditLogContext>().IsEnabled = false;

        var today = DateTime.Today;
        var now = DateTime.Now;
        var scanStart = today.AddDays(-ScanWindowDays);

        // 数据库层面追加时间窗口过滤（近 7 天）+ EndTime 已过，避免全表扫描与内存过滤
        var candidates = await dbContext.Appointments
            .Where(a => a.Status == AppointmentStatus.Confirmed
                && a.StartTime.Date >= scanStart
                && a.StartTime.Date <= today
                && a.EndTime != null && a.EndTime.Value < now)
            .ToListAsync(cancellationToken);

        if (candidates.Count == 0)
        {
            _logger.LogDebug("爽约扫描完成:候选 0 条,更新 0 条");
            return;
        }

        _logger.LogInformation("爽约扫描候选数量:{Count} 条", candidates.Count);

        // 按 TenantId 分组分批处理，避免单次 SaveChanges 数据量过大
        var tenantGroups = candidates.GroupBy(a => a.TenantId).ToList();
        var totalUpdated = 0;
        foreach (var tenantGroup in tenantGroups)
        {
            var batch = new List<Appointment>(BatchSize);
            foreach (var appointment in tenantGroup)
            {
                appointment.Status = AppointmentStatus.NoShow;
                appointment.UpdatedTime = now;
                batch.Add(appointment);

                if (batch.Count >= BatchSize)
                {
                    await dbContext.SaveChangesAsync(cancellationToken);
                    totalUpdated += batch.Count;
                    batch.Clear();
                }
            }

            if (batch.Count > 0)
            {
                await dbContext.SaveChangesAsync(cancellationToken);
                totalUpdated += batch.Count;
            }
        }

        _logger.LogInformation("爽约扫描完成:候选 {Candidate} 条,已标记爽约 {Updated} 条",
            candidates.Count, totalUpdated);
    }
}

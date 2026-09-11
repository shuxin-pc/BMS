using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Bms.BuildingBlocks.Core.Context;
using Bms.Store.Domain.Entities;
using Bms.Store.Infrastructure;

namespace Bms.Store.Api.BackgroundServices;

/// <summary>
/// 设备保养提醒自动生成定时任务
/// 每日凌晨 06:00 执行（避开 01:00 积分过期 / 02:00 日结 / 02:30 月度统计 / 03:00 库存预警扫描），
/// 扫描所有租户设备 NextMaintenanceDate 在未来 7 天内或已过期的设备，生成保养提醒记录。
/// 同一设备同一日期仅生成一条提醒，避免重复打扰；保养记录创建后由 EquipmentMaintenanceAppService 关闭对应提醒。
/// </summary>
public class EquipmentMaintenanceReminderService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EquipmentMaintenanceReminderService> _logger;

    /// <summary>
    /// 每日执行时刻（凌晨 06:00，避开其他定时任务）
    /// </summary>
    private static readonly TimeSpan ExecuteTime = new(6, 0, 0);

    /// <summary>
    /// 提醒生成阈值：设备下次保养日期在今天 + 此天数内时生成"即将到期"提醒
    /// </summary>
    private const int UpcomingDays = 7;

    public EquipmentMaintenanceReminderService(
        IServiceProvider serviceProvider,
        ILogger<EquipmentMaintenanceReminderService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// 定时执行保养提醒扫描任务
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("设备保养提醒自动生成任务已启动，执行时刻：每日 {Time}", ExecuteTime);

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
                await ScanAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "设备保养提醒自动生成任务执行异常");
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
    /// 扫描所有租户的设备并生成保养提醒
    /// 后台任务不依赖 ICurrentUser，直接跨租户扫描；提醒记录的 TenantId/StoreId 从设备复制
    /// </summary>
    private async Task ScanAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<StoreDbContext>();

        // 审计日志豁免：后台定时任务无用户操作语义，关闭审计避免脏日志
        scope.ServiceProvider.GetRequiredService<IAuditLogContext>().IsEnabled = false;

        var today = DateTime.Today;
        var upcomingThreshold = today.AddDays(UpcomingDays);

        // 查询需要提醒的设备：未删除、有效租户、有下次保养日且在阈值内（含已过期）
        var equipments = await dbContext.Equipments
            .Where(e => !e.IsDeleted
                && e.TenantId != 0
                && e.NextMaintenanceDate.HasValue
                && e.NextMaintenanceDate.Value <= upcomingThreshold)
            .Select(e => new
            {
                e.Id,
                e.TenantId,
                e.TenantCode,
                e.StoreId,
                e.StoreCode,
                e.NextMaintenanceDate
            })
            .ToListAsync(cancellationToken);

        var now = DateTime.Now;
        var created = 0;

        foreach (var eq in equipments)
        {
            // 去重：同设备同日已生成未处理提醒则跳过
            var exists = await dbContext.EquipmentMaintenanceReminders
                .AnyAsync(r => r.EquipmentId == eq.Id
                    && r.ReminderDate == today
                    && !r.IsHandled, cancellationToken);
            if (exists)
                continue;

            var targetDate = eq.NextMaintenanceDate!.Value;
            var reminderType = targetDate < today ? 2 : 1;

            dbContext.EquipmentMaintenanceReminders.Add(new EquipmentMaintenanceReminder
            {
                EquipmentId = eq.Id,
                ReminderDate = today,
                TargetMaintenanceDate = targetDate,
                ReminderType = reminderType,
                IsHandled = false,
                TenantId = eq.TenantId,
                TenantCode = eq.TenantCode,
                StoreId = eq.StoreId,
                StoreCode = eq.StoreCode,
                CreatedTime = now
            });
            created++;
        }

        if (created > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation("设备保养提醒扫描完成：扫描设备 {Scanned} 台，新增提醒 {Created} 条",
            equipments.Count, created);
    }
}

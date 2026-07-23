using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Bms.Store.Application.Services;
using Bms.Store.Domain.Entities;
using Bms.Store.Infrastructure;

namespace Bms.Store.Api.BackgroundServices;

/// <summary>
/// 日结自动汇总兜底定时任务
/// 每日凌晨 02:00 执行，补全最近 30 天所有缺失日期的日结记录（P-DS-09）
/// 已存在 Source=1（手动汇总）的日期跳过；Source=2（系统自动）的日期允许更新覆盖（P-DS-10）
/// </summary>
public class DailySettlementSummaryService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DailySettlementSummaryService> _logger;

    /// <summary>
    /// 每日执行时刻（凌晨 02:00）
    /// </summary>
    private static readonly TimeSpan ExecuteTime = new(2, 0, 0);

    /// <summary>
    /// 兜底回补天数（默认 30 天，覆盖假期停业等场景）
    /// </summary>
    private const int BackfillDays = 30;

    /// <summary>
    /// 批量提交阈值（每 100 条提交一次，避免单次事务过大）
    /// </summary>
    private const int BatchSaveThreshold = 100;

    public DailySettlementSummaryService(
        IServiceProvider serviceProvider,
        ILogger<DailySettlementSummaryService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// 定时执行汇总任务
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("日结自动汇总兜底任务已启动，执行时刻：每日 {Time}", ExecuteTime);

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
                await SummarizeRecentAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "日结自动汇总兜底任务执行异常");
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
    /// 对所有门店补全最近 N 天所有缺失日期的日结记录（P-DS-09）
    /// 跳过手动汇总（Source=1）；更新覆盖系统自动汇总（Source=2）（P-DS-10）
    /// </summary>
    private async Task SummarizeRecentAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<StoreDbContext>();
        var appService = scope.ServiceProvider.GetRequiredService<DailySettlementAppService>();

        var today = DateTime.Today;

        // 查询所有未删除门店
        var stores = await dbContext.Stores
            .Where(s => !s.IsDeleted)
            .Select(s => new { s.Id, s.Code, s.TenantId, s.TenantCode })
            .ToListAsync(cancellationToken);

        if (stores.Count == 0)
        {
            _logger.LogInformation("日结自动汇总：未发现门店，跳过");
            return;
        }

        var created = 0;
        var updated = 0;

        foreach (var store in stores)
        {
            // 遍历最近 BackfillDays 天，补全所有缺失日期
            for (int i = 1; i <= BackfillDays; i++)
            {
                var targetDate = today.AddDays(-i);
                if (targetDate >= today) continue; // 不补未来日期

                // P-DS-10: 检查是否存在手动汇总记录（Source=1），存在则跳过
                var manualExists = await dbContext.DailySettlements
                    .AnyAsync(s => s.TenantId == store.TenantId && s.StoreId == store.Id
                        && s.SettlementDate == targetDate && s.Source == 1, cancellationToken);
                if (manualExists) continue;

                // 查询是否已有自动汇总记录（Source=2）
                var autoExisting = await dbContext.DailySettlements
                    .FirstOrDefaultAsync(s => s.TenantId == store.TenantId && s.StoreId == store.Id
                        && s.SettlementDate == targetDate && s.Source == 2, cancellationToken);

                var data = await appService.SummarizeCoreAsync(store.TenantId, store.Id, targetDate, cancellationToken);

                if (autoExisting != null)
                {
                    // P-DS-10: 自动汇总记录允许覆盖更新
                    UpdateEntityFromSummary(autoExisting, data);
                    autoExisting.UpdatedTime = DateTime.Now;
                    updated++;
                }
                else
                {
                    // 创建新的自动记录
                    var entity = new DailySettlement
                    {
                        TenantId = store.TenantId,
                        TenantCode = store.TenantCode ?? string.Empty,
                        StoreId = store.Id,
                        StoreCode = store.Code ?? string.Empty,
                        SettlementDate = targetDate,
                        SettlementTime = DateTime.Now,
                        OperatorId = null,
                        TotalRevenue = data.TotalRevenue,
                        CashRevenue = data.CashRevenue,
                        StoredValueRevenue = data.StoredValueRevenue,
                        PointsDeductAmount = data.PointsDeductAmount,
                        TotalRefund = data.TotalRefund,
                        CashRefundAmount = data.CashRefundAmount,
                        TreatmentCardVerifyAmount = data.TreatmentCardVerifyAmount,
                        TotalStoredValueRecharge = data.TotalStoredValueRecharge,
                        TotalStoredValueConsume = data.TotalStoredValueConsume,
                        OrderCount = data.OrderCount,
                        TotalCost = data.TotalCost,
                        SalesOutboundCost = data.SalesOutboundCost,
                        TreatmentCardOutboundCost = data.TreatmentCardOutboundCost,
                        InventoryLossAmount = data.InventoryLossAmount,
                        SampleGiftAmount = data.SampleGiftAmount,
                        TransferOutAmount = data.TransferOutAmount,
                        TransferInAmount = data.TransferInAmount,
                        PurchaseReturnAmount = data.PurchaseReturnAmount,
                        TotalGrossProfit = data.TotalGrossProfit,
                        Status = 1, // 兜底直接生成已确认，可反日结修正
                        Source = 2, // 系统自动汇总
                        Remark = "系统自动汇总",
                        CreatedTime = DateTime.Now
                    };
                    dbContext.DailySettlements.Add(entity);
                    created++;
                }

                // 同步生成 DailyStat 记录（传入 tenantCode，因 BackgroundService 无 HttpContext）
                await appService.EnsureDailyStatAsync(store.TenantId, store.Id, targetDate, store.TenantCode);

                // 批量提交，避免单次事务过大
                if ((created + updated) % BatchSaveThreshold == 0)
                {
                    await dbContext.SaveChangesAsync(cancellationToken);
                }
            }
        }

        if (created > 0 || updated > 0)
            await dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "日结自动汇总完成：共检查 {Stores} 家门店 × {Days} 天，新建 {Created} 条，更新 {Updated} 条",
            stores.Count, BackfillDays, created, updated);
    }

    /// <summary>
    /// 用最新汇总数据更新已有日结实体字段
    /// </summary>
    private static void UpdateEntityFromSummary(DailySettlement entity, SettlementSummaryData data)
    {
        entity.TotalRevenue = data.TotalRevenue;
        entity.CashRevenue = data.CashRevenue;
        entity.StoredValueRevenue = data.StoredValueRevenue;
        entity.PointsDeductAmount = data.PointsDeductAmount;
        entity.TotalRefund = data.TotalRefund;
        entity.CashRefundAmount = data.CashRefundAmount;
        entity.TreatmentCardVerifyAmount = data.TreatmentCardVerifyAmount;
        entity.TotalStoredValueRecharge = data.TotalStoredValueRecharge;
        entity.TotalStoredValueConsume = data.TotalStoredValueConsume;
        entity.OrderCount = data.OrderCount;
        entity.TotalCost = data.TotalCost;
        entity.SalesOutboundCost = data.SalesOutboundCost;
        entity.TreatmentCardOutboundCost = data.TreatmentCardOutboundCost;
        entity.InventoryLossAmount = data.InventoryLossAmount;
        entity.SampleGiftAmount = data.SampleGiftAmount;
        entity.TransferOutAmount = data.TransferOutAmount;
        entity.TransferInAmount = data.TransferInAmount;
        entity.PurchaseReturnAmount = data.PurchaseReturnAmount;
        entity.TotalGrossProfit = data.TotalGrossProfit;
    }
}

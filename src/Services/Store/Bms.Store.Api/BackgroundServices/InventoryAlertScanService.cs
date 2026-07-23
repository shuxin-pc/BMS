using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Bms.Store.Application.Services;

namespace Bms.Store.Api.BackgroundServices;

/// <summary>
/// 库存预警自动扫描定时任务
/// 每日凌晨 03:00 执行，扫描低库存/效期/积压预警并自动生成预警记录
/// </summary>
public class InventoryAlertScanService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<InventoryAlertScanService> _logger;

    /// <summary>
    /// 每日执行时刻（凌晨 03:00，避开 02:00 的日结任务）
    /// </summary>
    private static readonly TimeSpan ExecuteTime = new(3, 0, 0);

    public InventoryAlertScanService(
        IServiceProvider serviceProvider,
        ILogger<InventoryAlertScanService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// 定时执行预警扫描任务
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("库存预警自动扫描任务已启动，执行时刻：每日 {Time}", ExecuteTime);

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
                _logger.LogError(ex, "库存预警自动扫描任务执行异常");
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
    /// 执行预警扫描
    /// </summary>
    private async Task ScanAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var appService = scope.ServiceProvider.GetRequiredService<IInventoryAlertAppService>();

        _logger.LogInformation("开始执行库存预警扫描...");
        var result = await appService.ScanAsync();

        if (result.IsSuccess && result.Data != null)
        {
            var d = result.Data;
            _logger.LogInformation(
                "库存预警扫描完成：低库存 {Low} 条、效期 {Expiry} 条、积压 {Over} 条、过期批次 {Batch} 个",
                d.LowStockCreated, d.ExpiryCreated, d.OverstockCreated, d.BatchExpired);
        }
        else
        {
            _logger.LogWarning("库存预警扫描返回失败：{Message}", result.Message);
        }
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Bms.BuildingBlocks.Storage.Abstractions;
using Bms.Store.Infrastructure;

namespace Bms.Store.Api.BackgroundServices;

/// <summary>
/// 孤儿对象清理任务（图片对象存储改造 阶段 4）
/// 每日 04:00 比对 MinIO 对象与数据库引用，删除无人引用的残留图片。
/// 孤儿有两个来源：用户上传后未提交表单；业务删除时删库成功而删对象失败
/// （该失败被有意降级为仅记日志、不回滚业务数据，就是为了由本任务兜底）
/// </summary>
public class OrphanObjectCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IOrphanObjectCleaner _cleaner;
    private readonly ILogger<OrphanObjectCleanupService> _logger;

    /// <summary>
    /// 每日执行时刻（04:00，避开 01:00–03:00 的积分、日结、月度统计与库存预警任务）
    /// </summary>
    private static readonly TimeSpan ExecuteTime = new(10, 0, 0);

    /// <summary>
    /// 保留期：上传不足 24 小时的对象不清理。
    /// 用户可能上传照片后长时间停留在编辑界面才提交，保留期过短会删掉即将被引用的图片
    /// </summary>
    private static readonly TimeSpan RetentionPeriod = TimeSpan.FromHours(24);

    /// <summary>
    /// 参与清理的业务类型。
    /// 只能列入引用已被 <see cref="LoadReferencedKeysAsync"/> 统计到的类型：
    /// 其余允许上传的 bizType（product-image / technician-avatar / purchase-voucher）尚未接入对象存储，
    /// 待它们接入时，必须在此登记的同时补充对应的引用查询，否则其对象会被误判为孤儿删除
    /// </summary>
    private static readonly string[] CleanupBizTypes = ["customer-photo"];

    public OrphanObjectCleanupService(
        IServiceProvider serviceProvider,
        IOrphanObjectCleaner cleaner,
        ILogger<OrphanObjectCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _cleaner = cleaner;
        _logger = logger;
    }

    /// <summary>
    /// 定时执行孤儿对象清理
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "孤儿对象清理任务已启动，执行时刻：每日 {Time}，保留期 {Retention}",
            ExecuteTime, RetentionPeriod);

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
                await CleanOrphanObjectsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "孤儿对象清理任务执行异常");
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
    /// 执行一轮清理。判定与删除均由类库完成，此处只提供引用来源与清理范围
    /// </summary>
    private async Task CleanOrphanObjectsAsync(CancellationToken cancellationToken)
    {
        var result = await _cleaner.CleanAsync(new OrphanCleanupRequest
        {
            BizTypes = CleanupBizTypes,
            RetentionPeriod = RetentionPeriod,
            LoadReferencedKeysAsync = LoadReferencedKeysAsync
        }, cancellationToken);

        _logger.LogInformation(
            "孤儿对象清理完成：扫描 {ScannedCount} 个对象，删除 {DeletedCount} 个孤儿",
            result.ScannedCount, result.DeletedCount);
    }

    /// <summary>
    /// 查出数据库中在用的照片来源。
    /// 目前仅服务对比照片明细（customer-photo）使用对象存储，故只查该表；
    /// 结果含外链值无妨——外链不会命中桶内对象键
    /// </summary>
    private async Task<ISet<string>> LoadReferencedKeysAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<StoreDbContext>();

        // IgnoreQueryFilters：清理是跨租户的运维操作，必须取到全量引用。
        // 漏掉任何一条引用即意味着误删仍在使用的图片，故不依赖运行时租户上下文
        var references = await dbContext.ServiceComparisonPhotoItems
            .IgnoreQueryFilters()
            .Select(item => item.PhotoSource)
            .Distinct()
            .ToListAsync(cancellationToken);

        return references.ToHashSet(StringComparer.Ordinal);
    }
}

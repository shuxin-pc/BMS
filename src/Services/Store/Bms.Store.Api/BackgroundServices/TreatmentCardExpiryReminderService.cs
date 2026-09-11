using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Bms.Store.Domain.Constants;
using Bms.Store.Domain.Entities;
using Bms.Store.Infrastructure;

namespace Bms.Store.Api.BackgroundServices;

/// <summary>
/// 项目卡到期提醒站内信自动发送定时任务
/// 每日 08:30 执行（避开生日 07:00 / 预约 08:00 / 设备保养 06:00 等任务），
/// 扫描各门店 30 天内到期或已过期的有效项目卡（Status=1），
/// 向各门店提醒配置（子表 StoreReminderSetting，ReminderType=TreatmentExpiry）中配置的接收角色发送站内信。
/// 提醒接收角色为门店级配置（每门店一条记录）：按门店扫描该门店的项目卡销售记录，使用该门店配置的角色发送。
/// 按销售记录+预警级别去重（BizType=TreatmentExpiryReminder, BizKey={saleId}:{alertLevel}）：
/// 每张卡进入 30 天到期窗口时发送一次"即将到期"提醒（AlertLevel=1），
/// 过期后若仍有剩余次数（Status 仍为有效）再发送一次"已到期"提醒（AlertLevel=2），
/// 服务中断恢复后只要仍在窗口内会自动补发，同一预警级别仅发送一次。
/// </summary>
public class TreatmentCardExpiryReminderService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TreatmentCardExpiryReminderService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    /// <summary>
    /// 每日执行时刻（08:30，避开其他定时任务，晚于预约提醒便于当日运营节奏）
    /// </summary>
    private static readonly TimeSpan ExecuteTime = new(8, 30, 0);

    /// <summary>
    /// 到期提醒窗口天数：扫描未来 N 天内（含当天）到期及已过期的项目卡
    /// 与到期提醒列表页（TreatmentCardExpiryQueryDto）口径一致
    /// </summary>
    private const int ReminderWindowDays = 30;

    /// <summary>
    /// 业务类型标识，与 System 服务 Message.BizType 配合用于去重
    /// </summary>
    private const string BizType = "TreatmentExpiryReminder";

    /// <summary>
    /// 来源子系统编码
    /// </summary>
    private const string SourceSubsystemCode = "Store";

    /// <summary>
    /// 消息分类：2=业务通知
    /// </summary>
    private const int MessageCategoryBusiness = 2;

    /// <summary>
    /// 目标类型：2=按角色
    /// </summary>
    private const int MessageTargetTypeRole = 2;

    /// <summary>
    /// 预警级别：1=即将到期
    /// </summary>
    private const int AlertLevelExpiring = 1;

    /// <summary>
    /// 预警级别：2=已到期
    /// </summary>
    private const int AlertLevelExpired = 2;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = null,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public TreatmentCardExpiryReminderService(
        IServiceProvider serviceProvider,
        ILogger<TreatmentCardExpiryReminderService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// 定时执行项目卡到期提醒扫描任务
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("项目卡到期提醒站内信自动发送任务已启动，执行时刻：每日 {Time}", ExecuteTime);

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
                _logger.LogError(ex, "项目卡到期提醒站内信自动发送任务执行异常");
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
    /// 扫描所有配置了项目卡到期提醒角色的门店，向各门店配置的角色发送到期站内信
    /// </summary>
    private async Task ScanAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<StoreDbContext>();
        var configuration = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();

        var systemApiUrl = configuration["SystemApi:BaseUrl"] ?? "http://localhost:5000";
        var internalServiceName = configuration["InternalServices:ServiceName"] ?? "StoreService";
        var internalServiceKey = configuration["InternalServices:ServiceKey"] ?? "StoreService-Internal-Key-2026";

        var httpClient = _httpClientFactory.CreateClient("SystemApi");
        httpClient.BaseAddress = new Uri(systemApiUrl);
        httpClient.DefaultRequestHeaders.Add("X-Internal-Service", internalServiceName);
        httpClient.DefaultRequestHeaders.Add("X-Internal-Service-Key", internalServiceKey);

        var now = DateTime.Now;
        var alertThreshold = now.AddDays(ReminderWindowDays);

        // 1. 查询所有配置了项目卡到期提醒角色的门店提醒配置（子表，每门店一条，含 TenantId/StoreId/RoleIds）
        // RoleIds 为 jsonb 列，EF 会把 Count>0 翻译为 cardinality(jsonb)，PostgreSQL 不支持该函数，
        // 故不在 SQL 中过滤 RoleIds 非空，先按 ReminderType 查出后在内存过滤（三提醒服务同一模式）
        var activeSettings = (await dbContext.StoreReminderSettings
            .Where(s => !s.IsDeleted
                && s.ReminderType == ReminderTypes.TreatmentExpiry)
            .ToListAsync(cancellationToken))
            .Where(s => s.RoleIds != null && s.RoleIds.Count > 0)
            .ToList();

        if (activeSettings.Count == 0)
        {
            _logger.LogInformation("项目卡到期提醒扫描完成：无门店配置项目卡到期提醒角色，跳过");
            return;
        }

        var totalSent = 0;
        var totalSkipped = 0;

        foreach (var setting in activeSettings)
        {
            var (sent, skipped) = await ProcessTenantAsync(
                dbContext, httpClient, setting, now, alertThreshold, cancellationToken);
            totalSent += sent;
            totalSkipped += skipped;
        }

        _logger.LogInformation("项目卡到期提醒扫描完成：扫描门店配置 {Stores} 条，发送站内信 {Sent} 条，跳过已发送 {Skipped} 条",
            activeSettings.Count, totalSent, totalSkipped);
    }

    /// <summary>
    /// 处理单个门店的项目卡到期提醒
    /// 查询该门店 30 天内到期或已过期的有效项目卡销售记录，使用该门店配置的接收角色发送
    /// </summary>
    private async Task<(int sent, int skipped)> ProcessTenantAsync(
        StoreDbContext dbContext,
        HttpClient httpClient,
        StoreReminderSetting setting,
        DateTime now,
        DateTime alertThreshold,
        CancellationToken cancellationToken)
    {
        var tenantId = setting.TenantId;
        var storeId = setting.StoreId;

        // 2. 查询该门店 30 天内到期或已过期的有效项目卡（Status=1，含已过期但未标记状态的卡）
        var sales = await dbContext.TreatmentCardSales
            .Where(s => s.TenantId == tenantId
                && s.StoreId == storeId
                && !s.IsDeleted
                && s.Status == 1
                && s.ExpiryDate <= alertThreshold)
            .Select(s => new
            {
                s.Id,
                s.CustomerId,
                s.CardId,
                s.ExpiryDate,
                s.RemainingTimes
            })
            .ToListAsync(cancellationToken);

        if (sales.Count == 0)
            return (0, 0);

        // 3. 批量查询客户与项目卡名称，避免 N+1
        var customerIds = sales.Select(s => s.CustomerId).Distinct().ToList();
        var cardIds = sales.Select(s => s.CardId).Distinct().ToList();

        var customers = await dbContext.Customers
            .Where(c => customerIds.Contains(c.Id) && !c.IsDeleted)
            .Select(c => new { c.Id, c.Name, c.Phone })
            .ToDictionaryAsync(c => c.Id, c => c, cancellationToken);

        var cards = await dbContext.TreatmentCards
            .Where(c => cardIds.Contains(c.Id))
            .Select(c => new { c.Id, c.Name })
            .ToDictionaryAsync(c => c.Id, c => c, cancellationToken);

        // 4. 计算每张卡的预警级别：已过期（2）/ 即将到期（1），并按级别生成 BizKey
        var reminders = sales
            .Select(s => new
            {
                Sale = s,
                AlertLevel = s.ExpiryDate < now ? AlertLevelExpired : AlertLevelExpiring,
                BizKey = $"{s.Id}:{(s.ExpiryDate < now ? AlertLevelExpired : AlertLevelExpiring)}"
            })
            .ToList();

        // 5. 批量检查已发送的 BizKey，避免重复发送
        var bizKeys = reminders.Select(r => r.BizKey).ToList();
        var existingKeys = await CheckBizExistsAsync(httpClient, bizKeys, cancellationToken);
        var existingSet = new HashSet<string>(existingKeys, StringComparer.Ordinal);

        // 6. 对未发送的记录逐个发送站内信
        var sent = 0;
        var skipped = 0;
        foreach (var r in reminders)
        {
            if (existingSet.Contains(r.BizKey))
            {
                skipped++;
                continue;
            }

            customers.TryGetValue(r.Sale.CustomerId, out var customer);
            cards.TryGetValue(r.Sale.CardId, out var card);

            var success = await SendExpiryNotifyAsync(
                httpClient, setting,
                r.Sale.Id,
                customer?.Name ?? string.Empty,
                customer?.Phone ?? string.Empty,
                card?.Name ?? string.Empty,
                r.Sale.ExpiryDate,
                r.Sale.RemainingTimes,
                r.AlertLevel,
                r.BizKey,
                cancellationToken);

            if (success)
                sent++;
            else
                skipped++;
        }

        return (sent, skipped);
    }

    /// <summary>
    /// 调用 System 服务批量检查 BizKey 是否已存在消息记录
    /// </summary>
    private async Task<List<string>> CheckBizExistsAsync(
        HttpClient httpClient, List<string> bizKeys, CancellationToken cancellationToken)
    {
        try
        {
            var requestBody = new { BizType = BizType, BizKeys = bizKeys };
            var response = await httpClient.PostAsJsonAsync(
                "/api/internal/messages/check-biz-exists", requestBody, JsonOptions, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("检查 BizKey 存在性失败，HTTP {Status}，本次跳过去重", response.StatusCode);
                return new List<string>();
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<CheckBizExistsResponse>(content, JsonOptions);
            return result?.Data?.ExistingBizKeys ?? new List<string>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "检查 BizKey 存在性异常，本次跳过去重");
            return new List<string>();
        }
    }

    /// <summary>
    /// 调用 System 服务发送单条项目卡到期提醒站内信
    /// 按预警级别区分文案：即将到期（联系客户及时使用）/ 已到期（联系客户处理剩余次数）
    /// </summary>
    private async Task<bool> SendExpiryNotifyAsync(
        HttpClient httpClient,
        StoreReminderSetting setting,
        long saleId,
        string customerName,
        string phone,
        string cardName,
        DateTime expiryDate,
        int remainingTimes,
        int alertLevel,
        string bizKey,
        CancellationToken cancellationToken)
    {
        string title;
        string content;
        if (alertLevel == AlertLevelExpiring)
        {
            var remainingDays = (expiryDate - DateTime.Now).Days;
            title = "项目卡到期提醒";
            content = $"客户{customerName}（手机号：{phone}）的项目卡「{cardName}」将于{expiryDate:yyyy-MM-dd}到期，" +
                $"剩余{remainingTimes}次，剩余{remainingDays}天。请及时联系客户使用。";
        }
        else
        {
            title = "项目卡已过期提醒";
            content = $"客户{customerName}（手机号：{phone}）的项目卡「{cardName}」已于{expiryDate:yyyy-MM-dd}到期，" +
                $"仍有{remainingTimes}次未使用。请及时联系客户处理。";
        }

        var targetUrl = "/store/treatment/expire";

        var requestBody = new
        {
            Title = title,
            Content = content,
            Category = MessageCategoryBusiness,
            SourceSubsystemCode = SourceSubsystemCode,
            TargetType = MessageTargetTypeRole,
            TargetIds = setting.RoleIds,
            TargetUrl = targetUrl,
            TenantId = setting.TenantId,
            BizType = BizType,
            BizKey = bizKey
        };

        try
        {
            var response = await httpClient.PostAsJsonAsync(
                "/api/internal/messages/notify", requestBody, JsonOptions, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("发送项目卡到期提醒站内信失败：销售记录 {SaleId}，HTTP {Status}",
                    saleId, response.StatusCode);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送项目卡到期提醒站内信异常：销售记录 {SaleId}", saleId);
            return false;
        }
    }

    /// <summary>
    /// System 服务 check-biz-exists 接口响应结构
    /// </summary>
    private class CheckBizExistsResponse
    {
        public int Code { get; set; }
        public CheckBizExistsData? Data { get; set; }
    }

    private class CheckBizExistsData
    {
        public List<string> ExistingBizKeys { get; set; } = new();
    }
}

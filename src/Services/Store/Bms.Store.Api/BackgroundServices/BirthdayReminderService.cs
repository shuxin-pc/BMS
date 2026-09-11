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
/// 客户生日提醒站内信自动发送定时任务
/// 每日凌晨 07:00 执行（避开 01:00 积分过期 / 02:00 日结 / 02:30 月度统计 / 03:00 库存预警 / 06:00 设备保养），
/// 扫描未来 3 天内（含当天）过生日的客户，向各门店提醒配置（子表 StoreReminderSetting，ReminderType=Birthday）中
/// 配置的生日提醒角色发送站内信。
/// 生日提醒角色为门店级配置（每门店一条记录）：按门店扫描该门店客户，使用该门店配置的角色发送。
/// 按客户+生日年份去重（BizType=BirthdayReminder, BizKey={customerId}:{year}），
/// 服务中断恢复后只要仍在 3 天窗口内会自动补发，同一客户同一年生日仅发送一次。
/// </summary>
public class BirthdayReminderService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BirthdayReminderService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    /// <summary>
    /// 每日执行时刻（07:00，避开其他定时任务）
    /// </summary>
    private static readonly TimeSpan ExecuteTime = new(7, 0, 0);

    /// <summary>
    /// 生日提醒窗口天数：扫描未来 N 天内（含当天）过生日的客户
    /// </summary>
    private const int ReminderWindowDays = 3;

    /// <summary>
    /// 业务类型标识，与 System 服务 Message.BizType 配合用于去重
    /// </summary>
    private const string BizType = "BirthdayReminder";

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

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = null,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public BirthdayReminderService(
        IServiceProvider serviceProvider,
        ILogger<BirthdayReminderService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// 定时执行生日提醒扫描任务
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("客户生日提醒站内信自动发送任务已启动，执行时刻：每日 {Time}", ExecuteTime);

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
                _logger.LogError(ex, "客户生日提醒站内信自动发送任务执行异常");
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
    /// 扫描所有门店配置的未来 3 天内过生日的客户，向各门店配置的角色发送站内信
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

        var today = DateTime.Today;

        // 1. 查询所有配置了生日提醒角色的门店提醒配置（子表，每门店一条，含 TenantId/StoreId/RoleIds）
        // RoleIds 为 jsonb 列，EF 会把 Count>0 翻译为 cardinality(jsonb)，PostgreSQL 不支持该函数，
        // 故不在 SQL 中过滤 RoleIds 非空，先按 ReminderType 查出后在内存过滤（三提醒服务同一模式）
        var activeSettings = (await dbContext.StoreReminderSettings
            .Where(s => !s.IsDeleted
                && s.ReminderType == ReminderTypes.Birthday)
            .ToListAsync(cancellationToken))
            .Where(s => s.RoleIds != null && s.RoleIds.Count > 0)
            .ToList();

        if (activeSettings.Count == 0)
        {
            _logger.LogInformation("生日提醒扫描完成：无门店配置生日提醒角色，跳过");
            return;
        }

        var totalSent = 0;
        var totalSkipped = 0;

        foreach (var setting in activeSettings)
        {
            var (sent, skipped) = await ProcessTenantAsync(
                dbContext, httpClient, setting, today, cancellationToken);
            totalSent += sent;
            totalSkipped += skipped;
        }

        _logger.LogInformation("生日提醒扫描完成：扫描门店配置 {Stores} 条，发送站内信 {Sent} 条，跳过已发送 {Skipped} 条",
            activeSettings.Count, totalSent, totalSkipped);
    }

    /// <summary>
    /// 处理单个门店的生日提醒
    /// 按该门店配置的 StoreId 过滤客户，使用该门店配置的生日提醒角色发送
    /// </summary>
    private async Task<(int sent, int skipped)> ProcessTenantAsync(
        StoreDbContext dbContext,
        HttpClient httpClient,
        StoreReminderSetting setting,
        DateTime today,
        CancellationToken cancellationToken)
    {
        var tenantId = setting.TenantId;
        var storeId = setting.StoreId;

        // 2. 查询该门店下所有有生日且未删除的客户
        var customers = await dbContext.Customers
            .Where(c => c.Birthday.HasValue
                && !c.IsDeleted
                && c.TenantId == tenantId
                && c.StoreId == storeId)
            .Select(c => new { c.Id, c.Name, c.Phone, c.Birthday })
            .ToListAsync(cancellationToken);

        if (customers.Count == 0)
            return (0, 0);

        // 3. 计算每个客户距下次生日的天数，过滤 [0, ReminderWindowDays]
        var reminders = customers
            .Select(c =>
            {
                var nextBirthday = GetNextBirthday(c.Birthday!.Value, today);
                var days = (nextBirthday - today).Days;
                return new
                {
                    c.Id,
                    c.Name,
                    c.Phone,
                    Birthday = c.Birthday!.Value,
                    NextBirthday = nextBirthday,
                    DaysToBirthday = days
                };
            })
            .Where(r => r.DaysToBirthday >= 0 && r.DaysToBirthday <= ReminderWindowDays)
            .ToList();

        if (reminders.Count == 0)
            return (0, 0);

        // 4. 批量检查已发送的 BizKey，避免重复发送
        var bizKeys = reminders
            .Select(r => $"{r.Id}:{r.NextBirthday.Year}")
            .ToList();

        var existingKeys = await CheckBizExistsAsync(httpClient, bizKeys, cancellationToken);
        var existingSet = new HashSet<string>(existingKeys, StringComparer.Ordinal);

        // 5. 对未发送的客户逐个发送站内信
        var sent = 0;
        var skipped = 0;
        foreach (var r in reminders)
        {
            var bizKey = $"{r.Id}:{r.NextBirthday.Year}";
            if (existingSet.Contains(bizKey))
            {
                skipped++;
                continue;
            }

            var success = await SendBirthdayNotifyAsync(
                httpClient, setting, r.Id, r.Name, r.Phone, r.Birthday, bizKey, cancellationToken);
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
    /// 调用 System 服务发送单条生日提醒站内信
    /// </summary>
    private async Task<bool> SendBirthdayNotifyAsync(
        HttpClient httpClient,
        StoreReminderSetting setting,
        long customerId,
        string customerName,
        string phone,
        DateTime birthday,
        string bizKey,
        CancellationToken cancellationToken)
    {
        var birthdayText = $"{birthday.Month}月{birthday.Day}日";
        var title = $"客户{customerName}将于{birthdayText}过生日";
        var content = $"客户{customerName}（手机号：{phone}）将于{birthdayText}过生日，请及时关怀。";
        var targetUrl = $"/store/customer/profile?customerId={customerId}";

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
                _logger.LogError("发送生日提醒站内信失败：客户 {CustomerId}，HTTP {Status}",
                    customerId, response.StatusCode);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送生日提醒站内信异常：客户 {CustomerId}", customerId);
            return false;
        }
    }

    /// <summary>
    /// 计算客户在当前年份的下一个生日日期
    /// 处理 2 月 29 日生日在非闰年的情况（按 2 月 28 日处理）
    /// </summary>
    private static DateTime GetNextBirthday(DateTime birthday, DateTime today)
    {
        var day = birthday.Day;
        if (birthday.Month == 2 && day == 29 && !DateTime.IsLeapYear(today.Year))
            day = 28;

        var nextBirthday = new DateTime(today.Year, birthday.Month, day);
        if (nextBirthday < today)
            nextBirthday = nextBirthday.AddYears(1);

        return nextBirthday;
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

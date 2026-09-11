using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
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
/// 今日预约提醒站内信自动发送定时任务
/// 每日 08:00（门店上班前）执行，扫描当天（自然日）状态为已预约的预约，
/// 向各门店提醒配置（子表 StoreReminderSetting，ReminderType=Appointment）中配置的接收角色发送站内信。
/// 提醒接收角色为门店级配置（每门店一条记录）：按门店扫描该门店当天预约，使用该门店配置的角色发送。
/// 按预约ID去重（BizType=AppointmentReminder, BizKey={appointmentId}），发送成功后更新预约 ReminderStatus=2，
/// 服务中断恢复后自动补发，同一预约仅发送一次。
/// </summary>
public class AppointmentReminderService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AppointmentReminderService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    /// <summary>
    /// 每日执行时刻（08:00，门店上班前送达今日预约安排）
    /// </summary>
    private static readonly TimeSpan ExecuteTime = new(8, 0, 0);

    /// <summary>
    /// 业务类型标识，与 System 服务 Message.BizType 配合用于去重
    /// </summary>
    private const string BizType = "AppointmentReminder";

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

    public AppointmentReminderService(
        IServiceProvider serviceProvider,
        ILogger<AppointmentReminderService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// 定时执行今日预约提醒扫描任务
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("今日预约提醒站内信自动发送任务已启动，执行时刻：每日 {Time}", ExecuteTime);

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
                _logger.LogError(ex, "今日预约提醒站内信自动发送任务执行异常");
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
    /// 扫描所有配置了预约提醒角色的门店，向各门店配置的角色发送当天预约站内信
    /// </summary>
    private async Task ScanAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<StoreDbContext>();

        // 审计日志豁免：后台定时任务无用户操作语义，关闭审计避免脏日志
        scope.ServiceProvider.GetRequiredService<IAuditLogContext>().IsEnabled = false;
        var configuration = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();

        var systemApiUrl = configuration["SystemApi:BaseUrl"] ?? "http://localhost:5000";
        var internalServiceName = configuration["InternalServices:ServiceName"] ?? "StoreService";
        var internalServiceKey = configuration["InternalServices:ServiceKey"] ?? "StoreService-Internal-Key-2026";

        var httpClient = _httpClientFactory.CreateClient("SystemApi");
        httpClient.BaseAddress = new Uri(systemApiUrl);
        httpClient.DefaultRequestHeaders.Add("X-Internal-Service", internalServiceName);
        httpClient.DefaultRequestHeaders.Add("X-Internal-Service-Key", internalServiceKey);

        var today = DateTime.Today;

        // 1. 查询所有配置了预约提醒角色的门店提醒配置（子表，每门店一条，含 TenantId/StoreId/RoleIds）
        // RoleIds 为 jsonb 列，EF 会把 Count>0 翻译为 cardinality(jsonb)，PostgreSQL 不支持该函数，
        // 故不在 SQL 中过滤 RoleIds 非空，先按 ReminderType 查出后在内存过滤（三提醒服务同一模式）
        var activeSettings = (await dbContext.StoreReminderSettings
            .Where(s => !s.IsDeleted
                && s.ReminderType == ReminderTypes.Appointment)
            .ToListAsync(cancellationToken))
            .Where(s => s.RoleIds != null && s.RoleIds.Count > 0)
            .ToList();

        if (activeSettings.Count == 0)
        {
            _logger.LogInformation("今日预约提醒扫描完成：无门店配置预约提醒角色，跳过");
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

        _logger.LogInformation("今日预约提醒扫描完成：扫描门店配置 {Stores} 条，发送站内信 {Sent} 条，跳过已发送 {Skipped} 条",
            activeSettings.Count, totalSent, totalSkipped);
    }

    /// <summary>
    /// 处理单个门店的今日预约提醒
    /// 查询该门店当天状态为已预约的预约，使用该门店配置的预约提醒角色发送
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

        // 2. 查询该门店当天状态为已预约的预约（Appointment 无软删除字段）
        var appointments = await dbContext.Appointments
            .Where(a => a.TenantId == tenantId
                && a.StoreId == storeId
                && a.StartTime.Date == today
                && a.Status == AppointmentStatus.Confirmed)
            .Select(a => new
            {
                a.Id,
                a.AppointmentNo,
                a.CustomerName,
                a.CustomerPhone,
                a.StartTime,
                a.ProductId,
                a.TechnicianId
            })
            .ToListAsync(cancellationToken);

        if (appointments.Count == 0)
            return (0, 0);

        // 3. 批量查询关联技师名称，避免 N+1
        var technicianIds = appointments.Where(a => a.TechnicianId.HasValue).Select(a => a.TechnicianId!.Value).Distinct().ToList();
        var technicianNames = await dbContext.Technicians
            .Where(t => technicianIds.Contains(t.Id))
            .Select(t => new { t.Id, t.Name })
            .ToDictionaryAsync(t => t.Id, t => t.Name, cancellationToken);

        // 4. 批量查询关联商品名称，避免 N+1（替代原 ServiceItem 字符串字段）
        var productIds = appointments.Select(a => a.ProductId).Distinct().ToList();
        var productNames = await dbContext.Products
            .Where(p => productIds.Contains(p.Id))
            .Select(p => new { p.Id, Name = p.Master.Name })
            .ToDictionaryAsync(p => p.Id, p => p.Name, cancellationToken);

        // 5. 批量检查已发送的 BizKey，避免重复发送
        var bizKeys = appointments.Select(a => $"{a.Id}").ToList();
        var existingKeys = await CheckBizExistsAsync(httpClient, bizKeys, cancellationToken);
        var existingSet = new HashSet<string>(existingKeys, StringComparer.Ordinal);

        // 6. 对未发送的预约逐个发送站内信，成功后更新预约提醒状态
        var sent = 0;
        var skipped = 0;
        foreach (var a in appointments)
        {
            var bizKey = $"{a.Id}";
            if (existingSet.Contains(bizKey))
            {
                skipped++;
                continue;
            }

            var serviceName = productNames.TryGetValue(a.ProductId, out var name) ? name : string.Empty;
            var technicianName = a.TechnicianId.HasValue && technicianNames.TryGetValue(a.TechnicianId.Value, out var techName)
                ? techName
                : string.Empty;

            var success = await SendAppointmentNotifyAsync(
                httpClient, setting, a.Id, a.AppointmentNo, a.CustomerName, a.CustomerPhone,
                serviceName, technicianName, a.StartTime, bizKey, cancellationToken);

            if (success)
            {
                // 发送成功后更新预约提醒状态（去重标记，避免本地重复发送；远端 BizKey 去重兜底）
                var entity = await dbContext.Appointments
                    .FirstOrDefaultAsync(x => x.Id == a.Id && x.TenantId == tenantId && x.StoreId == storeId, cancellationToken);
                if (entity != null)
                {
                    entity.ReminderStatus = 2;
                    entity.ReminderTime = DateTime.Now;
                    entity.UpdatedTime = DateTime.Now;
                }
                sent++;
            }
            else
            {
                skipped++;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
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
    /// 调用 System 服务发送单条今日预约提醒站内信
    /// </summary>
    private async Task<bool> SendAppointmentNotifyAsync(
        HttpClient httpClient,
        StoreReminderSetting setting,
        long appointmentId,
        string appointmentNo,
        string customerName,
        string phone,
        string serviceName,
        string technicianName,
        DateTime startTime,
        string bizKey,
        CancellationToken cancellationToken)
    {
        var appointmentTimeText = $"{startTime:yyyy-MM-dd HH:mm}";
        var title = "今日预约提醒";
        var content = $"预约编号：{appointmentNo}；客户：{customerName}（手机号：{phone}）；" +
            $"服务项目：{serviceName}；技师：{technicianName}；预约时间：{appointmentTimeText}。请提前做好接待准备。";
        var targetUrl = "/store/appointment/list";

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
                _logger.LogError("发送今日预约提醒站内信失败：预约 {AppointmentId}，HTTP {Status}",
                    appointmentId, response.StatusCode);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送今日预约提醒站内信异常：预约 {AppointmentId}", appointmentId);
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

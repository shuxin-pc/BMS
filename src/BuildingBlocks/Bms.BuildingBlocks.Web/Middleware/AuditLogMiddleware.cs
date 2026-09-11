using System.Diagnostics;
using Bms.BuildingBlocks.Abstractions.Security;
using Bms.BuildingBlocks.Core.Context;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Bms.BuildingBlocks.Web.Middleware;

/// <summary>
/// 审计日志中间件
/// 在请求开始时初始化审计日志上下文，结束时通过 OnCompleted 调用 IAuditLogWriter 写入审计日志
/// 门店信息从 HttpContext.Items["StoreId"]/["StoreName"] 读取（由业务服务的多租户/门店上下文中间件填充）
/// </summary>
public class AuditLogMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuditLogMiddleware> _logger;

    /// <summary>
    /// 默认排除路径（当配置为空时使用）
    /// </summary>
    private static readonly HashSet<string> DefaultExcludedPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/swagger",
        "/health",
        "/favicon.ico"
    };

    /// <summary>
    /// 强制排除路径前缀：内部服务调用（如更新最后登录时间）是系统行为，不属于用户操作审计，
    /// 与 OptionsProvider 返回的配置排除路径取并集生效，不受配置覆盖影响
    /// </summary>
    private static readonly List<string> ForcedExcludedPrefixes = new()
    {
        "/api/internal"
    };

    public AuditLogMiddleware(RequestDelegate next, ILogger<AuditLogMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IAuditLogContext auditLogContext, ICurrentUser currentUser)
    {
        _logger.LogInformation("[AuditLogMiddleware] 请求路径: {Path}, 方法: {Method}", context.Request.Path, context.Request.Method);

        // 检查是否启用了审计日志（由各服务的 OptionsProvider 决定读取来源）
        var optionsProvider = context.RequestServices.GetService<IAuditLogOptionsProvider>();
        var isAuditLogEnabled = optionsProvider != null
            ? await optionsProvider.IsEnabledAsync()
            : true;
        if (!isAuditLogEnabled)
        {
            _logger.LogInformation("[AuditLogMiddleware] 审计日志已禁用，跳过");
            await _next(context);
            return;
        }

        // 检查是否命中排除路径
        var path = context.Request.Path.Value ?? string.Empty;
        var excludedPaths = optionsProvider != null
            ? (await optionsProvider.GetExcludedPathsAsync()).ToList()
            : DefaultExcludedPaths.ToList();
        if (ShouldSkip(path, excludedPaths) || ShouldSkip(path, ForcedExcludedPrefixes))
        {
            _logger.LogInformation("[AuditLogMiddleware] 路径 {Path} 在排除列表中，跳过", path);
            await _next(context);
            return;
        }

        _logger.LogInformation("[AuditLogMiddleware] 开始处理请求，Path: {Path}", path);

        // 初始化审计日志上下文
        InitializeAuditLogContext(context, auditLogContext, currentUser);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            // 记录错误信息
            auditLogContext.ResponseStatus = StatusCodes.Status500InternalServerError;
            auditLogContext.ErrorMessage = ex.Message;
            throw;
        }
        finally
        {
            stopwatch.Stop();

            // 填充响应信息（用于后续 OnCompleted 回调）
            var responseStatus = context.Response.StatusCode;
            var duration = stopwatch.ElapsedMilliseconds;
            auditLogContext.ResponseStatus = responseStatus;
            auditLogContext.Duration = duration;
            auditLogContext.RequestTime ??= DateTime.Now.AddMilliseconds(-stopwatch.ElapsedMilliseconds);

            // 注册 OnCompleted 回调，在响应完成后写入审计日志
            // 此时 Response.StatusCode 已经确定，能获取到真实值
            // 注意：OnCompleted 回调由服务器在请求管道之外触发，读取不到管道内经 AsyncLocal
            // 存储的审计上下文（AuditLogContext 的待写入列表与字段值），必须在此处（请求 flow 内）
            // 完成快照，经闭包传给回调
            var writer = context.RequestServices.GetService<IAuditLogWriter>();
            var contextEntry = new AuditLogEntry
            {
                TenantId = auditLogContext.TenantId,
                StoreId = auditLogContext.StoreId,
                StoreName = auditLogContext.StoreName,
                UserId = auditLogContext.UserId,
                UserName = auditLogContext.UserName,
                RealName = auditLogContext.RealName,
                RequestPath = auditLogContext.RequestPath,
                RequestMethod = auditLogContext.RequestMethod,
                RequestIp = auditLogContext.RequestIp,
                UserAgent = auditLogContext.UserAgent,
                CreatedTime = auditLogContext.RequestTime ?? DateTime.Now
            };
            var pendingLogs = auditLogContext.PendingAuditLogs.ToList();
            var customOperationType = auditLogContext.CustomOperationType;

            context.Response.OnCompleted(async () =>
            {
                try
                {
                    await WriteAuditLogsAsync(writer, pendingLogs, customOperationType, contextEntry, responseStatus, duration);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[AuditLogMiddleware] OnCompleted 写入审计日志失败");
                }
            });

            _logger.LogInformation("[AuditLogMiddleware] 请求完成，Path: {Path}, Status: {Status}, Duration: {Duration}ms",
                context.Request.Path, context.Response.StatusCode, stopwatch.ElapsedMilliseconds);
        }
    }

    /// <summary>
    /// 写入待处理的审计日志
    /// 快照数据在请求 flow 内生成后经参数传入，回调中不可再读 AsyncLocal 存储的审计上下文
    /// </summary>
    private async Task WriteAuditLogsAsync(
        IAuditLogWriter? writer,
        IReadOnlyList<AuditLogEntry> pendingLogs,
        string? customOperationType,
        AuditLogEntry contextEntry,
        int responseStatus,
        long duration)
    {
        var hasCustomOperation = !string.IsNullOrEmpty(customOperationType);

        // 如果既没有待写入的日志，也没有自定义操作类型，则直接返回
        if (!pendingLogs.Any() && !hasCustomOperation)
        {
            return;
        }

        if (writer == null)
        {
            _logger.LogWarning("[AuditLogMiddleware] 未注册 IAuditLogWriter，审计日志丢弃，条数: {Count}", pendingLogs.Count);
            return;
        }

        // 拦截器收集的变更日志直接作为写入条目（响应状态码与耗时由 WriteAsync 参数补齐）
        var entries = new List<AuditLogEntry>(pendingLogs);

        // 写入自定义操作类型的审计日志（如登录、登出）
        if (hasCustomOperation)
        {
            entries.Add(new AuditLogEntry
            {
                TenantId = contextEntry.TenantId,
                StoreId = contextEntry.StoreId,
                StoreName = contextEntry.StoreName,
                UserId = contextEntry.UserId,
                UserName = contextEntry.UserName,
                RealName = contextEntry.RealName,
                OperationType = customOperationType!,
                OperationContent = customOperationType,
                RequestPath = contextEntry.RequestPath,
                RequestMethod = contextEntry.RequestMethod,
                RequestIp = contextEntry.RequestIp,
                UserAgent = contextEntry.UserAgent,
                CreatedTime = contextEntry.CreatedTime
            });
        }

        await writer.WriteAsync(entries, responseStatus, duration);

        _logger.LogInformation("[AuditLogMiddleware] OnCompleted 成功写入审计日志，条数: {Count}", entries.Count);
    }

    private void InitializeAuditLogContext(HttpContext context, IAuditLogContext auditLogContext, ICurrentUser currentUser)
    {
        var request = context.Request;

        // 用户信息
        auditLogContext.UserId = currentUser.UserId;
        auditLogContext.UserName = currentUser.UserName;
        auditLogContext.RealName = currentUser.RealName;
        auditLogContext.TenantId = currentUser.TenantId;

        // 门店信息（由业务服务的门店上下文中间件写入 HttpContext.Items，非门店服务为空）
        if (context.Items.TryGetValue("StoreId", out var storeIdObj) && storeIdObj is long storeId)
        {
            auditLogContext.StoreId = storeId;
        }
        if (context.Items.TryGetValue("StoreName", out var storeNameObj) && storeNameObj is string storeName)
        {
            auditLogContext.StoreName = storeName;
        }

        // 请求信息
        auditLogContext.RequestPath = request.Path.Value;
        auditLogContext.RequestMethod = request.Method;
        auditLogContext.RequestIp = GetClientIpAddress(context);
        auditLogContext.UserAgent = request.Headers.UserAgent.ToString();
        auditLogContext.RequestTime = DateTime.Now;

        // 响应信息初始值
        auditLogContext.ResponseStatus = null;
        auditLogContext.Duration = null;
        auditLogContext.ErrorMessage = null;
        auditLogContext.IsEnabled = true;

        // 清空待处理的审计日志
        auditLogContext.ClearPendingAuditLogs();
    }

    private static string? GetClientIpAddress(HttpContext context)
    {
        // 优先从 X-Forwarded-For 获取（反向代理场景）
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            // X-Forwarded-For 可能包含多个IP，取第一个
            return forwardedFor.Split(',')[0].Trim();
        }

        // 其次从 X-Real-IP 获取
        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        // 最后从 Connection.RemoteIpAddress 获取
        return context.Connection.RemoteIpAddress?.ToString();
    }

    private static bool ShouldSkip(string path, List<string> excludedPaths)
    {
        foreach (var excluded in excludedPaths)
        {
            if (path.StartsWith(excluded, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }
}

/// <summary>
/// 审计日志中间件扩展方法
/// </summary>
public static class AuditLogMiddlewareExtensions
{
    /// <summary>
    /// 使用审计日志中间件
    /// </summary>
    public static IApplicationBuilder UseAuditLog(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<AuditLogMiddleware>();
    }
}

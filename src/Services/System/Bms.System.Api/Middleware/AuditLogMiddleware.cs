using System.Diagnostics;
using Bms.BuildingBlocks.Abstractions.Security;
using Bms.BuildingBlocks.Core.Context;
using Bms.System.Domain.Entities;
using Bms.System.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Bms.System.Api.Middleware;

/// <summary>
/// 审计日志中间件
/// 在请求开始时初始化审计日志上下文，结束时通过 OnCompleted 写入审计日志
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

    public AuditLogMiddleware(RequestDelegate next, ILogger<AuditLogMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// 获取排除路径列表（从配置读取）
    /// </summary>
    private HashSet<string> GetExcludedPaths(IServiceProvider services)
    {
        // 从配置读取排除路径
        var systemConfigService = services.GetService<Application.Services.ISystemConfigService>();
        var excludePathsValue = systemConfigService?.GetValueAsync("AuditLogExcludePaths", "").GetAwaiter().GetResult() ?? "";

        if (string.IsNullOrWhiteSpace(excludePathsValue))
        {
            return DefaultExcludedPaths;
        }

        // 配置格式：逗号分隔的路径，如 "/health,/swagger,/favicon.ico"
        return excludePathsValue
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public async Task InvokeAsync(HttpContext context, IAuditLogContext auditLogContext, ICurrentUser currentUser)
    {
        // 调试日志
        _logger.LogInformation("[AuditLogMiddleware] 请求路径: {Path}, 方法: {Method}", context.Request.Path, context.Request.Method);
        _logger.LogInformation("[AuditLogMiddleware] IAuditLogContext: {Context}, ICurrentUser: {User}", auditLogContext != null, currentUser != null);

        // 检查是否需要记录审计日志（从配置读取排除路径）
        var path = context.Request.Path.Value ?? string.Empty;
        var excludedPaths = GetExcludedPaths(context.RequestServices);
        if (ShouldSkip(path, excludedPaths))
        {
            _logger.LogInformation("[AuditLogMiddleware] 路径 {Path} 在排除列表中，跳过", path);
            await _next(context);
            return;
        }

        // 检查是否启用了审计日志（从系统配置读取）
        var systemConfigService = context.RequestServices.GetService<Application.Services.ISystemConfigService>();
        var isAuditLogEnabled = systemConfigService?.GetBoolAsync("EnableAuditLog", true).GetAwaiter().GetResult() ?? true;
        if (!isAuditLogEnabled)
        {
            _logger.LogInformation("[AuditLogMiddleware] 审计日志已通过系统配置禁用，跳过");
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
            context.Response.OnCompleted(async () =>
            {
                try
                {
                    await WriteAuditLogsAsync(context, auditLogContext, responseStatus, duration);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[AuditLogMiddleware] OnCompleted 写入审计日志失败");
                }
            });

            _logger.LogInformation("[AuditLogMiddleware] 请求完成，Path: {Path}, Status: {Status}, Duration: {Duration}ms",
                context.Request.Path, context.Response.StatusCode, stopwatch.ElapsedMilliseconds);

            _logger.LogDebug(
                "审计日志 - User:{UserId}/{UserName}, Path:{Path}, Method:{Method}, Status:{Status}, Duration:{Duration}ms",
                auditLogContext.UserId,
                auditLogContext.UserName,
                auditLogContext.RequestPath,
                auditLogContext.RequestMethod,
                auditLogContext.ResponseStatus,
                auditLogContext.Duration);
        }
    }

    /// <summary>
    /// 写入待处理的审计日志
    /// </summary>
    private async Task WriteAuditLogsAsync(HttpContext context, IAuditLogContext auditLogContext, int responseStatus, long duration)
    {
        var pendingLogs = auditLogContext.PendingAuditLogs;
        var hasCustomOperation = !string.IsNullOrEmpty(auditLogContext.CustomOperationType);

        // 如果既没有待写入的日志，也没有自定义操作类型，则直接返回
        if (!pendingLogs.Any() && !hasCustomOperation)
        {
            return;
        }

        _logger.LogInformation("[AuditLogMiddleware] OnCompleted 开始写入审计日志，PendingLogs: {Count}, CustomOp: {CustomOp}",
            pendingLogs.Count, auditLogContext.CustomOperationType);

        // 获取 IServiceScopeFactory 来创建 DbContext
        var scopeFactory = context.RequestServices.GetRequiredService<IServiceScopeFactory>();
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SystemDbContext>();

        // 写入拦截器收集的审计日志
        foreach (var entry in pendingLogs)
        {
            var auditLog = new AuditLog
            {
                TenantId = entry.TenantId,
                UserId = entry.UserId,
                UserName = entry.UserName,
                RealName = entry.RealName,
                OperationType = entry.OperationType,
                OperationContent = entry.OperationContent,
                RequestPath = entry.RequestPath,
                RequestMethod = entry.RequestMethod,
                RequestIp = entry.RequestIp,
                UserAgent = entry.UserAgent,
                ResponseStatus = responseStatus,
                Duration = duration,
                EntityChanges = entry.EntityChanges,
                CreatedTime = entry.CreatedTime ?? DateTime.Now
            };
            dbContext.Set<AuditLog>().Add(auditLog);
        }

        // 写入自定义操作类型的审计日志（如登录、登出）
        if (hasCustomOperation)
        {
            var customLog = new AuditLog
            {
                TenantId = auditLogContext.TenantId,
                UserId = auditLogContext.UserId,
                UserName = auditLogContext.UserName,
                RealName = auditLogContext.RealName,
                OperationType = auditLogContext.CustomOperationType,
                OperationContent = $"{auditLogContext.CustomOperationType} operation",
                RequestPath = auditLogContext.RequestPath,
                RequestMethod = auditLogContext.RequestMethod,
                RequestIp = auditLogContext.RequestIp,
                UserAgent = auditLogContext.UserAgent,
                ResponseStatus = responseStatus,
                Duration = duration,
                EntityChanges = null,
                CreatedTime = auditLogContext.RequestTime ?? DateTime.Now
            };
            dbContext.Set<AuditLog>().Add(customLog);
        }

        // 写入数据库
        await dbContext.SaveChangesAsync();

        // 清空待处理的审计日志
        auditLogContext.ClearPendingAuditLogs();

        _logger.LogInformation("[AuditLogMiddleware] OnCompleted 成功写入审计日志");
    }

    private void InitializeAuditLogContext(HttpContext context, IAuditLogContext auditLogContext, ICurrentUser currentUser)
    {
        var request = context.Request;

        // 用户信息
        auditLogContext.UserId = currentUser.UserId;
        auditLogContext.UserName = currentUser.UserName;
        auditLogContext.RealName = currentUser.RealName;
        auditLogContext.TenantId = currentUser.TenantId;

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

    private static bool ShouldSkip(string path, HashSet<string> excludedPaths)
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

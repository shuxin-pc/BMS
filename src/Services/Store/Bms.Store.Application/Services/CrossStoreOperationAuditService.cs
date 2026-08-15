using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Bms.BuildingBlocks.Abstractions.Security;
using Bms.Store.Domain.Entities;
using Bms.Store.Infrastructure;

namespace Bms.Store.Application.Services;

/// <summary>
/// 跨店权益操作审计日志服务实现
/// 在调用方事务内写入审计日志，自动从 HttpContext 捕获 IP/UserAgent
/// 审计日志为 append-only，不提供修改/删除接口
/// </summary>
public class CrossStoreOperationAuditService : ICrossStoreOperationAuditService
{
    private readonly StoreDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CrossStoreOperationAuditService(
        StoreDbContext dbContext,
        IHttpContextAccessor httpContextAccessor)
    {
        _dbContext = dbContext;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// 写入跨店操作审计日志（在调用方事务内执行，不独立 SaveChanges）
    /// </summary>
    public Task LogAsync(CrossStoreOperationLog logEntry)
    {
        // 从 HttpContext 补充请求上下文信息（IP、UserAgent）
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            logEntry.RequestIp = httpContext.Connection.RemoteIpAddress?.ToString();
            logEntry.UserAgent = httpContext.Request.Headers.UserAgent.ToString();
        }

        logEntry.CreatedTime = DateTime.Now;
        _dbContext.CrossStoreOperationLogs.Add(logEntry);
        return Task.CompletedTask;
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Bms.BuildingBlocks.Abstractions.Security;
using Bms.System.Application.Dtos;
using Bms.System.Application.Dtos.AuditLogs;
using Bms.System.Application.Services;

namespace Bms.System.Api.Controllers;

[ApiController]
[Route("api/system/audit-logs")]
[Route("api/audit-logs")]
[Authorize]
public class AuditLogsController : ControllerBase
{
    private readonly IAuditLogAppService _auditLogService;
    private readonly ISystemConfigService _systemConfigService;
    private readonly ILogger<AuditLogsController> _logger;
    private readonly ICurrentUser _currentUser;

    /// <summary>
    /// 默认保留天数
    /// </summary>
    private const int DefaultRetentionDays = 30;

    public AuditLogsController(
        IAuditLogAppService auditLogService,
        ISystemConfigService systemConfigService,
        ILogger<AuditLogsController> logger,
        ICurrentUser currentUser)
    {
        _auditLogService = auditLogService;
        _systemConfigService = systemConfigService;
        _logger = logger;
        _currentUser = currentUser;
    }

    /// <summary>
    /// 获取审计日志详情
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ApiResponseDto<AuditLogDto?>> GetById(long id)
    {
        return await _auditLogService.GetByIdAsync(id);
    }

    /// <summary>
    /// 分页获取审计日志列表
    /// </summary>
    [HttpGet]
    public async Task<ApiResponseDto<PagedResponseDto<AuditLogDto>>> GetPagedList([FromQuery] AuditLogQueryDto query)
    {
        return await _auditLogService.GetPagedListAsync(query);
    }

    /// <summary>
    /// 创建审计日志（用于记录登录/登出等操作）
    /// </summary>
    [HttpPost]
    public async Task<ApiResponseDto> Create([FromBody] CreateAuditLogDto dto)
    {
        // 检查是否是登录/登出类型的审计日志，如果是则检查 EnableLoginAudit 配置
        // 由 Service 层处理租户优先级：优先读取同租户私有配置，其次读取公开配置
        if (IsLoginLogoutOperation(dto.OperationType))
        {
            var isLoginAuditEnabled = await _systemConfigService.GetBoolAsync(
                "EnableLoginAudit", true, _currentUser.TenantId);
            if (!isLoginAuditEnabled)
            {
                _logger.LogInformation("[AuditLog] 登录审计已禁用，跳过记录");
                return ApiResponseDto.Success(null, "skipped");
            }
        }

        // 从请求上下文获取 IP 和 UserAgent
        dto.RequestIp = GetClientIpAddress();
        dto.UserAgent = Request.Headers.UserAgent.ToString();

        return await _auditLogService.CreateAsync(dto);
    }

    /// <summary>
    /// 判断是否是登录/登出操作类型
    /// </summary>
    private static bool IsLoginLogoutOperation(string operationType)
    {
        return operationType.Equals("Login", StringComparison.OrdinalIgnoreCase) ||
               operationType.Equals("Logout", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 获取客户端IP地址
    /// </summary>
    private string? GetClientIpAddress()
    {
        // 优先从 X-Forwarded-For 获取（反向代理场景）
        var forwardedFor = Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        // 其次从 X-Real-IP 获取
        var realIp = Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        // 最后从 Connection.RemoteIpAddress 获取
        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }

    /// <summary>
    /// 删除审计日志
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ApiResponseDto> Delete(long id)
    {
        return await _auditLogService.DeleteAsync(id);
    }

    /// <summary>
    /// 清理过期审计日志（后端根据系统配置计算删除日期）
    /// </summary>
    [HttpDelete("expired")]
    public async Task<ApiResponseDto> DeleteExpired()
    {
        // 通过配置服务获取审计日志保留天数，支持缓存，默认30天
        var daysToKeep = await _systemConfigService.GetIntAsync("AuditLogRetentionDays", DefaultRetentionDays);

        var beforeDate = DateTime.Now.AddDays(-daysToKeep).Date;
        return await _auditLogService.DeleteExpiredAsync(beforeDate);
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Bms.System.Application.Dtos.Auth;
using Bms.System.Application.Services;
using Bms.System.Domain.Entities;
using Bms.System.Infrastructure;

namespace Bms.System.Api.Controllers;

/// <summary>
/// 内部认证控制器（供Identity.Api调用）
/// 通过 InternalServiceAuthMiddleware 校验 X-Internal-Service headers
/// </summary>
[ApiController]
[Route("api/internal/auth")]
[ApiExplorerSettings(IgnoreApi = true)] // 不在Swagger中显示
[Authorize] // 内部接口需认证（由 InternalServiceAuthMiddleware 设置身份）
public class InternalAuthController : ControllerBase
{
    private readonly IAuthAppService _authAppService;
    private readonly ILogger<InternalAuthController> _logger;
    private readonly SystemDbContext _dbContext;
    private readonly ISystemConfigService _systemConfigService;

    public InternalAuthController(
        IAuthAppService authAppService,
        ILogger<InternalAuthController> logger,
        SystemDbContext dbContext,
        ISystemConfigService systemConfigService)
    {
        _authAppService = authAppService;
        _logger = logger;
        _dbContext = dbContext;
        _systemConfigService = systemConfigService;
    }

    /// <summary>
    /// 验证用户凭据（内部接口）
    /// </summary>
    [HttpPost("validate")]
    public async Task<ValidateUserResponseDto> ValidateUser([FromBody] ValidateUserRequestDto request)
    {
        var result = await _authAppService.ValidateUserAsync(request);

        return result;
    }

    /// <summary>
    /// 记录登录/登出审计日志（内部接口）
    /// </summary>
    [HttpPost("audit-login")]
    public async Task<IActionResult> RecordLoginAudit([FromBody] LoginAuditRequestDto request)
    {
        // 检查是否启用了登录审计，由 Service 层处理租户优先级
        var tenantId = request.TenantId ?? 1;
        var isLoginAuditEnabled = await _systemConfigService.GetBoolAsync(
            "EnableLoginAudit", true, tenantId);
        if (!isLoginAuditEnabled)
        {
            _logger.LogInformation("[AuditLog] 登录审计已禁用，跳过记录");
            return Ok(new { success = true, skipped = true });
        }

        try
        {
            var auditLog = new AuditLog
            {
                TenantId = request.TenantId,
                UserId = request.UserId,
                UserName = request.UserName,
                RealName = request.RealName,
                OperationType = request.OperationType,
                OperationContent = $"{request.OperationType} operation",
                RequestPath = request.RequestPath,
                RequestMethod = "POST",
                RequestIp = request.RequestIp,
                UserAgent = request.UserAgent,
                ResponseStatus = request.ResponseStatus,
                Duration = 0,
                EntityChanges = null,
                CreatedTime = DateTime.Now
            };

            _dbContext.Set<AuditLog>().Add(auditLog);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("登录审计日志记录成功");
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "记录登录审计日志失败");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// 更新用户最后登录信息（内部接口）
    /// </summary>
    [HttpPost("update-last-login")]
    public async Task<IActionResult> UpdateLastLogin([FromBody] UpdateLastLoginRequestDto request)
    {
        try
        {
            var user = await _dbContext.Set<User>().FindAsync(request.UserId);
            if (user == null)
            {
                _logger.LogWarning("更新最后登录信息失败：用户不存在，UserId={UserId}", request.UserId);
                return NotFound(new { success = false, error = "用户不存在" });
            }

            user.LastLoginTime = request.LastLoginTime;
            user.LastLoginIp = request.LastLoginIp;

            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("用户最后登录信息更新成功，UserId={UserId}, LastLoginTime={LastLoginTime}, LastLoginIp={LastLoginIp}",
                request.UserId, request.LastLoginTime, request.LastLoginIp);
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新最后登录信息失败，UserId={UserId}", request.UserId);
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// 刷新令牌时获取用户最新状态（内部接口）
    /// 供 Identity.Api 在处理 refresh_token 授权类型时调用，确保用户被禁用或权限变更后旧 Token 无法刷新
    /// </summary>
    [HttpPost("refresh-user-info")]
    public async Task<ValidateUserResponseDto> RefreshUserInfo([FromBody] RefreshUserInfoRequestDto request)
    {
        _logger.LogInformation("刷新令牌获取用户最新状态，UserId={UserId}", request.UserId);
        return await _authAppService.GetUserForRefreshAsync(request.UserId);
    }
}

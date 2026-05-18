namespace Bms.System.Application.Dtos.Auth;

/// <summary>
/// 用户验证请求DTO
/// </summary>
public class ValidateUserRequestDto
{
    /// <summary>
    /// 用户名
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// 密码
    /// </summary>
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// 用户验证响应DTO
/// </summary>
public class ValidateUserResponseDto
{
    /// <summary>
    /// 是否验证成功
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// 用户ID
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// 用户名
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// 真实姓名
    /// </summary>
    public string? RealName { get; set; }

    /// <summary>
    /// 邮箱
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// 手机号
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// 头像
    /// </summary>
    public string? Avatar { get; set; }

    /// <summary>
    /// 租户ID
    /// </summary>
    public long TenantId { get; set; }

    /// <summary>
    /// 租户代码
    /// </summary>
    public string? TenantCode { get; set; }

    /// <summary>
    /// 角色列表
    /// </summary>
    public List<string> Roles { get; set; } = new();

    /// <summary>
    /// 角色ID列表
    /// </summary>
    public List<long> RoleIds { get; set; } = new();

    /// <summary>
    /// 权限列表
    /// </summary>
    public List<string> Permissions { get; set; } = new();

    /// <summary>
    /// 错误消息
    /// </summary>
    public string? ErrorMessage { get; set; }

    public static ValidateUserResponseDto Fail(string errorMessage)
    {
        return new ValidateUserResponseDto
        {
            IsValid = false,
            ErrorMessage = errorMessage
        };
    }

    public static ValidateUserResponseDto Success(long userId, string userName, long tenantId, string? tenantCode = null)
    {
        return new ValidateUserResponseDto
        {
            IsValid = true,
            UserId = userId,
            UserName = userName,
            TenantId = tenantId,
            TenantCode = tenantCode
        };
    }
}

/// <summary>
/// 登录审计日志请求DTO
/// </summary>
public class LoginAuditRequestDto
{
    /// <summary>
    /// 操作类型：Login 或 Logout
    /// </summary>
    public string OperationType { get; set; } = string.Empty;

    /// <summary>
    /// 用户ID
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// 用户名
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// 真实姓名
    /// </summary>
    public string? RealName { get; set; }

    /// <summary>
    /// 租户ID
    /// </summary>
    public long? TenantId { get; set; }

    /// <summary>
    /// 请求IP
    /// </summary>
    public string? RequestIp { get; set; }

    /// <summary>
    /// 用户代理
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// 响应状态码
    /// </summary>
    public int ResponseStatus { get; set; }

    /// <summary>
    /// 请求路径
    /// </summary>
    public string? RequestPath { get; set; }
}

/// <summary>
/// 更新最后登录信息请求DTO
/// </summary>
public class UpdateLastLoginRequestDto
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// 最后登录时间
    /// </summary>
    public DateTime LastLoginTime { get; set; }

    /// <summary>
    /// 最后登录IP
    /// </summary>
    public string? LastLoginIp { get; set; }
}

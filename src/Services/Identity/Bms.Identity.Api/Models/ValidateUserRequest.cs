namespace Bms.Identity.Api.Models;

/// <summary>
/// 用户验证请求
/// </summary>
public class ValidateUserRequest
{
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// 用户验证响应
/// </summary>
public class ValidateUserResponse
{
    public bool IsValid { get; set; }

    /// <summary>
    /// 用户ID（System API 返回字符串格式以避免精度丢失）
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    public string UserName { get; set; } = string.Empty;
    public string? RealName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Avatar { get; set; }

    /// <summary>
    /// 租户ID（System API 返回字符串格式以避免精度丢失）
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// 租户代码
    /// </summary>
    public string? TenantCode { get; set; }

    public List<string> Roles { get; set; } = new();
    public List<string> Permissions { get; set; } = new();
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// 登录审计日志请求
/// </summary>
public class LoginAuditRequest
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

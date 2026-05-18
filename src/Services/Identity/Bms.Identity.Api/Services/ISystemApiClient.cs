using Bms.Identity.Api.Models;

namespace Bms.Identity.Api.Services;

/// <summary>
/// System.Api 客户端接口
/// </summary>
public interface ISystemApiClient
{
    /// <summary>
    /// 验证用户凭据
    /// </summary>
    Task<ValidateUserResponse> ValidateUserAsync(string userName, string password);

    /// <summary>
    /// 记录登录/登出审计日志
    /// </summary>
    Task RecordLoginAuditAsync(LoginAuditRequest request);

    /// <summary>
    /// 更新用户最后登录信息
    /// </summary>
    Task UpdateLastLoginAsync(long userId, DateTime lastLoginTime, string? lastLoginIp);
}

using Bms.System.Application.Dtos.Auth;

namespace Bms.System.Application.Services;

/// <summary>
/// 认证应用服务接口
/// </summary>
public interface IAuthAppService
{
    /// <summary>
    /// 验证用户凭据
    /// </summary>
    Task<ValidateUserResponseDto> ValidateUserAsync(ValidateUserRequestDto request);

    /// <summary>
    /// 刷新令牌时获取用户最新状态（不验证密码，仅校验用户/租户状态并返回最新角色与权限）
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>用户最新信息；用户不存在/被禁用/租户禁用或过期时返回 Fail</returns>
    Task<ValidateUserResponseDto> GetUserForRefreshAsync(long userId);

    /// <summary>
    /// 获取当前用户信息
    /// </summary>
    Task<CurrentUserDto> GetCurrentUserAsync(long userId);
}

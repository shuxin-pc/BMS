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
    /// 获取当前用户信息
    /// </summary>
    Task<CurrentUserDto> GetCurrentUserAsync(long userId);
}

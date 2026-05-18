using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using Bms.System.Application.Dtos;
using Bms.System.Application.Dtos.Auth;
using Bms.System.Application.Services;

namespace Bms.System.Api.Controllers;

/// <summary>
/// 认证控制器
/// </summary>
[ApiController]
[Route("api/system/[controller]")]
[Route("api/[controller]")]
[Authorize]
public class AuthController : ControllerBase
{
    private readonly IAuthAppService _authAppService;

    public AuthController(IAuthAppService authAppService)
    {
        _authAppService = authAppService;
    }

    /// <summary>
    /// 获取当前登录用户信息
    /// </summary>
    [HttpGet("me")]
    public async Task<ApiResponseDto<CurrentUserDto>> GetCurrentUser()
    {
        // 先尝试从 user_id claim 获取
        var userIdClaim = User.FindFirst("user_id");
        if (userIdClaim != null && long.TryParse(userIdClaim.Value, out long userId))
        {
            var user = await _authAppService.GetCurrentUserAsync(userId);
            return ApiResponseDto<CurrentUserDto>.Success(user);
        }

        // 再尝试从 sub claim 获取
        var subClaim = User.FindFirst("sub");
        if (subClaim != null && long.TryParse(subClaim.Value, out userId))
        {
            var user = await _authAppService.GetCurrentUserAsync(userId);
            return ApiResponseDto<CurrentUserDto>.Success(user);
        }

        return ApiResponseDto<CurrentUserDto>.Fail("无法获取用户信息", 401);
    }
}

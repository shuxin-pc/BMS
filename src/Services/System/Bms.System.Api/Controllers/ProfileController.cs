using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Bms.System.Application.Dtos;
using Bms.System.Application.Dtos.Profile;
using Bms.System.Application.Services;

namespace Bms.System.Api.Controllers;

[ApiController]
[Route("api/system/[controller]")]
[Route("api/[controller]")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly IProfileAppService _profileService;

    public ProfileController(IProfileAppService profileService)
    {
        _profileService = profileService;
    }

    private long GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !long.TryParse(userIdClaim.Value, out var userId))
        {
            throw new UnauthorizedAccessException("无法获取当前用户信息");
        }
        return userId;
    }

    /// <summary>
    /// 获取当前用户信息
    /// </summary>
    [HttpGet]
    public async Task<ApiResponseDto<ProfileDto>> GetProfile()
    {
        var userId = GetCurrentUserId();
        return await _profileService.GetProfileAsync(userId);
    }

    /// <summary>
    /// 更新当前用户信息
    /// </summary>
    [HttpPut]
    public async Task<ApiResponseDto<ProfileDto>> UpdateProfile([FromBody] UpdateProfileDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            return await _profileService.UpdateProfileAsync(userId, dto);
        }
        catch (InvalidOperationException ex)
        {
            return ApiResponseDto<ProfileDto>.Fail(ex.Message, 400);
        }
    }

    /// <summary>
    /// 修改密码
    /// </summary>
    [HttpPut("password")]
    public async Task<ApiResponseDto> ChangePassword([FromBody] Bms.System.Application.Dtos.Profile.ChangePasswordDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            return await _profileService.ChangePasswordAsync(userId, dto);
        }
        catch (InvalidOperationException ex)
        {
            return ApiResponseDto.Fail(ex.Message, 400);
        }
    }

    /// <summary>
    /// 更新头像
    /// </summary>
    [HttpPut("avatar")]
    public async Task<ApiResponseDto<ProfileDto>> UpdateAvatar([FromBody] UpdateAvatarDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            return await _profileService.UpdateAvatarAsync(userId, dto);
        }
        catch (InvalidOperationException ex)
        {
            return ApiResponseDto<ProfileDto>.Fail(ex.Message, 400);
        }
    }
}

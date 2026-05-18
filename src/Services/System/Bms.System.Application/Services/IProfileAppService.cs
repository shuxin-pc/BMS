using Bms.System.Application.Dtos;
using Bms.System.Application.Dtos.Profile;

namespace Bms.System.Application.Services;

public interface IProfileAppService
{
    Task<ApiResponseDto<ProfileDto>> GetProfileAsync(long userId);
    Task<ApiResponseDto<ProfileDto>> UpdateProfileAsync(long userId, UpdateProfileDto dto);
    Task<ApiResponseDto> ChangePasswordAsync(long userId, ChangePasswordDto dto);
    Task<ApiResponseDto<ProfileDto>> UpdateAvatarAsync(long userId, UpdateAvatarDto dto);
}

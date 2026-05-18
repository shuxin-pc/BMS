using Bms.System.Application.Dtos;
using Bms.System.Application.Dtos.Profile;
using Bms.System.Domain.Entities;
using Bms.System.Domain.IRepositories;
using Bms.System.Domain.Interfaces;

namespace Bms.System.Application.Services;

public class ProfileAppService : IProfileAppService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITenantAppService _tenantAppService;

    public ProfileAppService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ITenantAppService tenantAppService)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _tenantAppService = tenantAppService;
    }

    public async Task<ApiResponseDto<ProfileDto>> GetProfileAsync(long userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            return ApiResponseDto<ProfileDto>.Fail("用户不存在", 404);
        }

        // 获取租户名称
        string? tenantName = null;
        try
        {
            var tenantResult = await _tenantAppService.GetByIdAsync(user.TenantId);
            if (tenantResult.Code == 200 && tenantResult.Data != null)
            {
                tenantName = tenantResult.Data.Name;
            }
        }
        catch
        {
            // 忽略租户名称获取失败
        }

        return ApiResponseDto<ProfileDto>.Success(MapToProfileDto(user, tenantName));
    }

    public async Task<ApiResponseDto<ProfileDto>> UpdateProfileAsync(long userId, UpdateProfileDto dto)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new InvalidOperationException("用户不存在");
        }

        if (!string.IsNullOrEmpty(dto.RealName))
        {
            user.RealName = dto.RealName;
        }
        if (!string.IsNullOrEmpty(dto.Email))
        {
            // 检查邮箱是否被其他用户使用
            var existingByEmail = await _userRepository.GetByEmailAsync(dto.Email);
            if (existingByEmail != null && existingByEmail.Id != userId)
            {
                throw new InvalidOperationException("邮箱已被其他用户使用");
            }
            user.Email = dto.Email;
        }
        if (!string.IsNullOrEmpty(dto.Phone))
        {
            // 检查手机号是否被其他用户使用
            var existingByPhone = await _userRepository.GetByPhoneAsync(dto.Phone);
            if (existingByPhone != null && existingByPhone.Id != userId)
            {
                throw new InvalidOperationException("手机号已被其他用户使用");
            }
            user.Phone = dto.Phone;
        }

        await _userRepository.UpdateAsync(user);

        // 获取租户名称
        string? tenantName = null;
        try
        {
            var tenantResult = await _tenantAppService.GetByIdAsync(user.TenantId);
            if (tenantResult.Code == 200 && tenantResult.Data != null)
            {
                tenantName = tenantResult.Data.Name;
            }
        }
        catch
        {
            // 忽略租户名称获取失败
        }

        return ApiResponseDto<ProfileDto>.Success(MapToProfileDto(user, tenantName), "更新成功");
    }

    public async Task<ApiResponseDto> ChangePasswordAsync(long userId, ChangePasswordDto dto)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new InvalidOperationException("用户不存在");
        }

        // 验证旧密码
        if (!_passwordHasher.VerifyPassword(dto.OldPassword, user.PasswordHash))
        {
            throw new InvalidOperationException("旧密码错误");
        }

        // 验证新密码强度
        if (dto.NewPassword.Length < 6)
        {
            throw new InvalidOperationException("新密码长度不能少于6位");
        }

        // 更新密码
        user.PasswordHash = _passwordHasher.HashPassword(dto.NewPassword);
        await _userRepository.UpdateAsync(user);

        return ApiResponseDto.Success(null, "密码修改成功");
    }

    public async Task<ApiResponseDto<ProfileDto>> UpdateAvatarAsync(long userId, UpdateAvatarDto dto)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new InvalidOperationException("用户不存在");
        }

        user.Avatar = dto.Avatar;
        await _userRepository.UpdateAsync(user);

        // 获取租户名称
        string? tenantName = null;
        try
        {
            var tenantResult = await _tenantAppService.GetByIdAsync(user.TenantId);
            if (tenantResult.Code == 200 && tenantResult.Data != null)
            {
                tenantName = tenantResult.Data.Name;
            }
        }
        catch
        {
            // 忽略租户名称获取失败
        }

        return ApiResponseDto<ProfileDto>.Success(MapToProfileDto(user, tenantName), "头像更新成功");
    }

    private ProfileDto MapToProfileDto(User user, string? tenantName = null)
    {
        // 从用户的 UserRoles 导航属性中获取角色信息
        var roles = user.UserRoles?
            .Where(ur => ur.Role != null && !ur.Role.IsDeleted)
            .Select(ur => new ProfileRoleDto
            {
                Id = ur.Role!.Id,
                Name = ur.Role.Name
            })
            .ToList() ?? new List<ProfileRoleDto>();

        return new ProfileDto
        {
            Id = user.Id,
            UserName = user.UserName,
            RealName = user.RealName,
            Email = user.Email,
            Phone = user.Phone,
            Avatar = user.Avatar,
            LastLoginTime = user.LastLoginTime,
            LastLoginIp = user.LastLoginIp,
            CreatedTime = user.CreatedTime,
            OrganizationName = user.Organization?.Name,
            TenantName = tenantName,
            Roles = roles
        };
    }
}

using Bms.System.Application.Dtos.Auth;
using Bms.System.Domain.Entities;
using Bms.System.Domain.Enums;
using Bms.System.Domain.Interfaces;
using Bms.System.Domain.IRepositories;

namespace Bms.System.Application.Services;

/// <summary>
/// 认证应用服务
/// </summary>
public class AuthAppService : IAuthAppService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;

    public AuthAppService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<ValidateUserResponseDto> ValidateUserAsync(ValidateUserRequestDto request)
    {
        // 查找用户
        var user = await _userRepository.GetByUserNameAsync(request.UserName);
        if (user == null)
        {
            return ValidateUserResponseDto.Fail("用户不存在");
        }

        // 检查用户状态
        if (user.Status != (int)UserStatus.Normal)
        {
            return ValidateUserResponseDto.Fail("用户已被禁用");
        }

        // 验证密码
        var passwordValid = _passwordHasher.VerifyPassword(request.Password, user.PasswordHash);
        if (!passwordValid)
        {
            return ValidateUserResponseDto.Fail("密码错误");
        }

        // 构建成功响应
        var response = ValidateUserResponseDto.Success(user.Id, user.UserName, user.TenantId, user.TenantCode);
        response.RealName = user.RealName;
        response.Email = user.Email;
        response.Phone = user.Phone;
        response.Avatar = user.Avatar;

        // 获取用户角色
        var roles = await _userRepository.GetUserRolesAsync(user.Id);
        response.Roles = roles.Select(r => r.Code).ToList();
        response.RoleIds = roles.Select(r => r.Id).ToList();

        // 获取用户权限
        var permissions = await _userRepository.GetUserPermissionsAsync(user.Id);
        response.Permissions = permissions.Select(p => p.Code).ToList();

        return response;
    }

    public async Task<CurrentUserDto> GetCurrentUserAsync(long userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new InvalidOperationException("用户不存在");
        }

        var roles = await _userRepository.GetUserRolesAsync(userId);
        var permissions = await _userRepository.GetUserPermissionsAsync(userId);

        return new CurrentUserDto
        {
            Id = user.Id,
            UserName = user.UserName,
            RealName = user.RealName,
            Email = user.Email,
            Phone = user.Phone,
            Avatar = user.Avatar,
            TenantId = user.TenantId,
            TenantCode = user.TenantCode,
            Roles = roles.Select(r => r.Code).ToList(),
            RoleIds = roles.Select(r => r.Id).ToList(),
            Permissions = permissions.Select(p => p.Code).ToList()
        };
    }
}

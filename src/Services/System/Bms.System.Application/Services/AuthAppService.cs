using Bms.BuildingBlocks.MultiTenant.Abstractions;
using Bms.System.Application.Dtos.Auth;
using Bms.System.Domain.Entities;
using Bms.System.Domain.Enums;
using Bms.System.Domain.Interfaces;
using Bms.System.Domain.IRepositories;
using Microsoft.Extensions.Logging;

namespace Bms.System.Application.Services;

/// <summary>
/// 认证应用服务
/// </summary>
public class AuthAppService : IAuthAppService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITenantStore _tenantStore;
    private readonly ILogger<AuthAppService> _logger;

    public AuthAppService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ITenantStore tenantStore,
        ILogger<AuthAppService> logger)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _tenantStore = tenantStore;
        _logger = logger;
    }

    public async Task<ValidateUserResponseDto> ValidateUserAsync(ValidateUserRequestDto request)
    {
        // 查找用户
        var user = await _userRepository.GetByUserNameAsync(request.UserName);
        if (user == null)
        {
            return ValidateUserResponseDto.Fail("用户名或密码错误");
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
            return ValidateUserResponseDto.Fail("用户名或密码错误");
        }

        // 提前获取用户角色，用于判断是否为超级管理员
        var roles = await _userRepository.GetUserRolesAsync(user.Id);
        var isSuperAdmin = roles.Any(r => string.Equals(r.Code, "super_admin", StringComparison.OrdinalIgnoreCase));

        // super_admin 跳过租户状态检查（平台管理员不属于任何业务租户）
        if (!isSuperAdmin)
        {
            // 检查租户状态
            var tenant = await _tenantStore.GetTenantByIdAsync(user.TenantId);

            if (tenant is { IsEnabled: false })
            {
                return ValidateUserResponseDto.Fail("账户已被禁用，无法登录");
            }

            // 检查租户是否过期（只精确到年月日，当天结束前都算有效）
            if (tenant?.ExpireTime.HasValue == true && tenant.ExpireTime.Value.Date < DateTime.Now.Date)
            {
                return ValidateUserResponseDto.Fail("账户已过期，无法登录");
            }
        }

        // 构建成功响应
        var response = ValidateUserResponseDto.Success(user.Id, user.UserName, user.TenantId, user.TenantCode);
        response.RealName = user.RealName;
        response.Email = user.Email;
        response.Phone = user.Phone;
        response.Avatar = user.Avatar;

        // 复用已获取的角色信息
        response.Roles = roles.Select(r => r.Code).ToList();
        response.RoleIds = roles.Select(r => r.Id).ToList();

        // 获取用户权限
        var permissions = await _userRepository.GetUserPermissionsAsync(user.Id);
        response.Permissions = permissions;
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

        // MaxRoleLevel：数字越小权限越大。无角色时视为 100（普通角色默认值）
        var maxRoleLevel = roles.Any() ? roles.Min(r => r.Level) : 100;

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
            Permissions = permissions,
            MaxRoleLevel = maxRoleLevel
        };
    }

    /// <summary>
    /// 刷新令牌时获取用户最新状态（不验证密码，仅校验用户/租户状态并返回最新角色与权限）
    /// </summary>
    public async Task<ValidateUserResponseDto> GetUserForRefreshAsync(long userId)
    {
        // 查找用户
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            return ValidateUserResponseDto.Fail("用户不存在");
        }

        // 检查用户状态
        if (user.Status != (int)UserStatus.Normal)
        {
            return ValidateUserResponseDto.Fail("用户已被禁用");
        }

        // 获取用户角色，用于判断是否为超级管理员
        var roles = await _userRepository.GetUserRolesAsync(user.Id);
        var isSuperAdmin = roles.Any(r => string.Equals(r.Code, "super_admin", StringComparison.OrdinalIgnoreCase));

        // super_admin 跳过租户状态检查（平台管理员不属于任何业务租户）
        if (!isSuperAdmin)
        {
            // 检查租户状态
            var tenant = await _tenantStore.GetTenantByIdAsync(user.TenantId);

            if (tenant is { IsEnabled: false })
            {
                return ValidateUserResponseDto.Fail("账户已被禁用，无法登录");
            }

            // 检查租户是否过期（只精确到年月日，当天结束前都算有效）
            if (tenant?.ExpireTime.HasValue == true && tenant.ExpireTime.Value.Date < DateTime.Now.Date)
            {
                return ValidateUserResponseDto.Fail("账户已过期，无法登录");
            }
        }

        // 构建成功响应
        var response = ValidateUserResponseDto.Success(user.Id, user.UserName, user.TenantId, user.TenantCode);
        response.RealName = user.RealName;
        response.Email = user.Email;
        response.Phone = user.Phone;
        response.Avatar = user.Avatar;

        // 复用已获取的角色信息
        response.Roles = roles.Select(r => r.Code).ToList();
        response.RoleIds = roles.Select(r => r.Id).ToList();

        // 获取用户权限
        var permissions = await _userRepository.GetUserPermissionsAsync(user.Id);
        response.Permissions = permissions;
        return response;
    }
}

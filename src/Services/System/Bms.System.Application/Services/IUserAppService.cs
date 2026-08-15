using Bms.System.Application.Dtos;
using Bms.System.Application.Dtos.Users;
using Bms.System.Application.Dtos.Roles;

namespace Bms.System.Application.Services;

public interface IUserAppService
{
    /// <summary>
    /// 获取用户分页列表
    /// </summary>
    Task<ApiResponseDto<PagedResponseDto<UserDto>>> GetPagedListAsync(PagedRequestDto request);

    /// <summary>
    /// 获取用户列表（带租户隔离和数据权限过滤）
    /// </summary>
    /// <param name="tenantId">租户ID筛选</param>
    /// <param name="realNameFilter">姓名筛选</param>
    /// <param name="userId">用户ID筛选（仅本人模式，数据权限 ScopeType=Self）</param>
    /// <param name="organizationIds">组织ID列表筛选（部门及以下/自定义模式）</param>
    /// <param name="creatorTenantId">创建者租户ID筛选（屏蔽平台跨租户创建的用户）</param>
    Task<ApiResponseDto<List<UserDto>>> GetAllListAsync(long? tenantId, string? realNameFilter, long? userId = null, List<long>? organizationIds = null, long? creatorTenantId = null);

    /// <summary>
    /// 获取用户详情
    /// </summary>
    Task<ApiResponseDto<UserDto?>> GetByIdAsync(long id);

    /// <summary>
    /// 创建用户
    /// </summary>
    Task<ApiResponseDto<UserDto>> CreateAsync(UserCreateDto dto, UserCreateContext context);

    /// <summary>
    /// 更新用户
    /// </summary>
    Task<ApiResponseDto<UserDto>> UpdateAsync(UserUpdateDto dto);

    /// <summary>
    /// 删除用户
    /// </summary>
    Task<ApiResponseDto> DeleteAsync(long id);

    /// <summary>
    /// 批量删除用户
    /// </summary>
    Task<ApiResponseDto> BatchDeleteAsync(List<long> ids);

    /// <summary>
    /// 重置密码
    /// </summary>
    Task<ApiResponseDto> ResetPasswordAsync(long id, string newPassword);

    /// <summary>
    /// 修改密码
    /// </summary>
    Task<ApiResponseDto> ChangePasswordAsync(long userId, string oldPassword, string newPassword);

    /// <summary>
    /// 分配角色
    /// </summary>
    Task<ApiResponseDto> AssignRolesAsync(long userId, List<long> roleIds);

    /// <summary>
    /// 获取用户角色
    /// </summary>
    Task<ApiResponseDto<List<UserRoleDto>>> GetUserRolesAsync(long userId);
}
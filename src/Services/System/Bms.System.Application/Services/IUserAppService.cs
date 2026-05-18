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
    /// 获取用户列表（带租户隔离）
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    /// <param name="realNameFilter">姓名筛选</param>
    Task<ApiResponseDto<List<UserDto>>> GetAllListAsync(long? tenantId, string? realNameFilter);

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
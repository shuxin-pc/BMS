using Microsoft.EntityFrameworkCore;
using Bms.System.Application.Dtos;
using Bms.System.Application.Dtos.Users;
using Bms.System.Application.Dtos.Roles;
using Bms.System.Domain.Entities;
using Bms.System.Domain.IRepositories;
using Bms.System.Domain.Interfaces;
using Bms.System.Infrastructure;

namespace Bms.System.Application.Services;

public class UserAppService : IUserAppService
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly SystemDbContext _context;

    public UserAppService(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IOrganizationRepository organizationRepository,
        IPasswordHasher passwordHasher,
        SystemDbContext context)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _organizationRepository = organizationRepository;
        _passwordHasher = passwordHasher;
        _context = context;
    }

    public async Task<ApiResponseDto<PagedResponseDto<UserDto>>> GetPagedListAsync(PagedRequestDto request)
    {
        // 调试日志
        Console.WriteLine($"[UserAppService] OrganizationIds received: {request.OrganizationIds?.Count ?? 0}");
        if (request.OrganizationIds != null && request.OrganizationIds.Any())
        {
            Console.WriteLine($"[UserAppService] OrganizationIds values: {string.Join(", ", request.OrganizationIds)}");
        }

        // 使用关键字或专用字段搜索
        var userName = !string.IsNullOrEmpty(request.Keyword) ? request.Keyword : request.UserName;
        var realName = request.RealName;

        var users = await _userRepository.GetPagedListAsync(
            request.PageIndex,
            request.PageSize,
            userName,
            realName,
            request.Status,
            request.TenantId,
            request.OrganizationId,
            request.UserId,
            request.OrganizationIds,
            request.RoleId);

        var totalCount = await _userRepository.GetCountAsync(userName, realName, request.Status, request.TenantId, request.OrganizationId, request.UserId, request.OrganizationIds, request.RoleId);

        var userDtos = new List<UserDto>();
        foreach (var user in users)
        {
            var dto = MapToDto(user);
            if (user.OrganizationId.HasValue)
            {
                var org = await _organizationRepository.GetByIdAsync(user.OrganizationId.Value);
                dto.OrganizationName = org?.Name;
            }
            // 获取租户名称
            if (user.TenantId > 0)
            {
                var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == user.TenantId);
                dto.TenantName = tenant?.Name;
            }
            // 获取用户角色名称
            if (user.UserRoles != null && user.UserRoles.Any())
            {
                var roleDtos = user.UserRoles
                    .Where(ur => ur.Role != null)
                    .Select(ur => new UserRoleDto
                    {
                        Id = ur.Role.Id,
                        Name = ur.Role.Name,
                        Code = ur.Role.Code
                    }).ToList();
                dto.Roles = roleDtos;
                // 设置角色名称用于列表显示
                dto.RoleNames = string.Join(",", roleDtos.Select(r => r.Name));
            }
            userDtos.Add(dto);
        }

        var result = new PagedResponseDto<UserDto>
        {
            List = userDtos,
            Total = totalCount,
            PageIndex = request.PageIndex,
            PageSize = request.PageSize
        };

        return ApiResponseDto<PagedResponseDto<UserDto>>.Success(result);
    }

    public async Task<ApiResponseDto<List<UserDto>>> GetAllListAsync(long? tenantId, string? realNameFilter)
    {
        var users = await _userRepository.GetListAsync(tenantId);

        // 按姓名筛选（忽略大小写）
        if (!string.IsNullOrWhiteSpace(realNameFilter))
        {
            users = users.Where(u => u.RealName != null && u.RealName.ToLower().Contains(realNameFilter.ToLower())).ToList();
        }

        var userDtos = users.Select(u => new UserDto
        {
            Id = u.Id,
            UserName = u.UserName,
            RealName = u.RealName,
            Email = u.Email,
            Phone = u.Phone,
            Status = u.Status,
            OrganizationId = u.OrganizationId
        }).ToList();

        return ApiResponseDto<List<UserDto>>.Success(userDtos);
    }

    public async Task<ApiResponseDto<UserDto?>> GetByIdAsync(long id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
        {
            return ApiResponseDto<UserDto?>.Fail("用户不存在", 404);
        }

        var dto = MapToDto(user);
        if (user.OrganizationId.HasValue)
        {
            var org = await _organizationRepository.GetByIdAsync(user.OrganizationId.Value);
            dto.OrganizationName = org?.Name;
        }

        // 获取用户角色
        var roles = await _roleRepository.GetByUserIdAsync(id);
        dto.Roles = roles.Select(r => new UserRoleDto
        {
            Id = r.Id,
            Name = r.Name,
            Code = r.Code
        }).ToList();

        return ApiResponseDto<UserDto?>.Success(dto);
    }

    public async Task<ApiResponseDto<UserDto>> CreateAsync(UserCreateDto dto, UserCreateContext context)
    {
        // 验证用户名唯一性
        if (await _userRepository.ExistsUserNameAsync(dto.UserName))
        {
            throw new InvalidOperationException($"用户名 {dto.UserName} 已存在");
        }

        // 验证邮箱唯一性
        if (!string.IsNullOrEmpty(dto.Email) && await _userRepository.ExistsEmailAsync(dto.Email))
        {
            throw new InvalidOperationException($"邮箱 {dto.Email} 已存在");
        }

        // 验证手机号唯一性
        if (!string.IsNullOrEmpty(dto.Phone) && await _userRepository.ExistsPhoneAsync(dto.Phone))
        {
            throw new InvalidOperationException($"手机号 {dto.Phone} 已存在");
        }

        var user = new User
        {
            UserName = dto.UserName,
            RealName = dto.RealName,
            Email = dto.Email,
            Phone = dto.Phone,
            PasswordHash = _passwordHasher.HashPassword(dto.Password),
            Avatar = dto.Avatar,
            Status = dto.Status,
            OrganizationId = dto.OrganizationId
        };

        // 根据当前用户角色设置租户ID
        if (context.IsSuperAdmin)
        {
            // 超级管理员：允许选择租户（使用请求中的租户ID，如果提供了的话）
            long? parsedTenantId = null;
            if (!string.IsNullOrEmpty(dto.TenantId) && long.TryParse(dto.TenantId, out long parsed))
            {
                parsedTenantId = parsed;
                user.TenantId = parsedTenantId.Value;
                // 查询租户表获取真实的租户编码
                var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == parsedTenantId.Value);
                user.TenantCode = tenant?.Code ?? parsedTenantId.Value.ToString();
            }
            else
            {
                // 未指定租户时，使用当前登录用户的租户信息
                user.TenantId = context.CurrentTenantId;
                user.TenantCode = context.CurrentTenantCode;
            }
        }
        else
        {
            // 管理员或其他角色：自动使用当前用户的租户ID
            user.TenantId = context.CurrentTenantId;
            user.TenantCode = context.CurrentTenantCode;
        }

        await _userRepository.AddAsync(user);

        // 分配角色
        if (dto.RoleIds.Any())
        {
            foreach (var roleId in dto.RoleIds)
            {
                var userRole = new UserRole
                {
                    UserId = user.Id,
                    RoleId = roleId
                };
                user.UserRoles.Add(userRole);
            }
            await _userRepository.UpdateAsync(user);
        }

        var result = await GetByIdAsync(user.Id);
        if (result.Data == null)
        {
            throw new InvalidOperationException("创建用户失败");
        }
        return ApiResponseDto<UserDto>.Success(result.Data, "创建成功");
    }

    public async Task<ApiResponseDto<UserDto>> UpdateAsync(UserUpdateDto dto)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(dto.Id);
            if (user == null)
            {
                throw new InvalidOperationException("用户不存在");
            }

            // 验证用户名唯一性
            if (await _userRepository.ExistsUserNameAsync(dto.UserName, dto.Id))
            {
                throw new InvalidOperationException($"用户名 {dto.UserName} 已存在");
            }

            // 验证邮箱唯一性
            if (!string.IsNullOrEmpty(dto.Email) && await _userRepository.ExistsEmailAsync(dto.Email, dto.Id))
            {
                throw new InvalidOperationException($"邮箱 {dto.Email} 已存在");
            }

            // 验证手机号唯一性
            if (!string.IsNullOrEmpty(dto.Phone) && await _userRepository.ExistsPhoneAsync(dto.Phone, dto.Id))
            {
                throw new InvalidOperationException($"手机号 {dto.Phone} 已存在");
            }

            // 先将实体从跟踪中分离，避免后续修改时被跟踪
            _context.Entry(user).State = EntityState.Detached;

            // 保存用户ID到本地变量
            var userId = user.Id;

            // 判断是否需要修改租户ID
            long? newTenantId = null;
            string? newTenantCode = null;
            if (!string.IsNullOrEmpty(dto.TenantId) && long.TryParse(dto.TenantId, out long parsedTenantId))
            {
                newTenantId = parsedTenantId;
                // 根据租户ID获取租户代码
                var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == newTenantId.Value);
                if (tenant != null)
                {
                    newTenantCode = tenant.Code;
                }
            }

            // 创建新的用户对象用于更新
            var userToUpdate = new User
            {
                Id = userId,
                UserName = user.UserName,
                RealName = dto.RealName,
                Email = dto.Email,
                Phone = dto.Phone,
                Avatar = dto.Avatar,
                Status = dto.Status,
                OrganizationId = dto.OrganizationId,
                TenantId = newTenantId ?? user.TenantId,
                TenantCode = newTenantCode ?? user.TenantCode,
                PasswordHash = user.PasswordHash,
                CreatedTime = user.CreatedTime,
                UpdatedTime = DateTime.UtcNow
            };

            // 使用 Attach 更新用户，手动控制哪些字段可以修改
            var entry = _context.Users.Attach(userToUpdate);
            entry.State = EntityState.Modified;

            // 如果传入了新的租户ID，则允许修改租户ID和租户代码
            if (newTenantId.HasValue)
            {
                entry.Property(e => e.TenantId).IsModified = true;
                entry.Property(e => e.TenantCode).IsModified = true;
            }
            else
            {
                entry.Property(e => e.TenantId).IsModified = false;
                entry.Property(e => e.TenantCode).IsModified = false;
            }

            entry.Property(e => e.UserName).IsModified = false;
            entry.Property(e => e.PasswordHash).IsModified = false;
            entry.Property(e => e.CreatedTime).IsModified = false;

            // 先保存用户基本信息
            await _context.SaveChangesAsync();

            // 单角色模式：直接修改现有 UserRole 的 RoleId
            var userRole = await _context.UserRoles
                .FirstOrDefaultAsync(ur => ur.UserId == userId);

            if (userRole != null)
            {
                // 已有角色，直接修改
                if (dto.RoleIds.Any())
                {
                    userRole.RoleId = dto.RoleIds[0];
                }
                else
                {
                    // 没传角色，删除关联
                    _context.UserRoles.Remove(userRole);
                }
            }
            else if (dto.RoleIds.Any())
            {
                // 没有现有角色，但传了新角色，创建关联
                _context.UserRoles.Add(new UserRole
                {
                    UserId = userId,
                    RoleId = dto.RoleIds[0]
                });
            }

            await _context.SaveChangesAsync();

            var result = await GetByIdAsync(userId);
            if (result.Data == null)
            {
                throw new InvalidOperationException("更新用户失败");
            }
            return ApiResponseDto<UserDto>.Success(result.Data, "更新成功");
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<ApiResponseDto> DeleteAsync(long id)
    {
        await _userRepository.DeleteAsync(id);
        return ApiResponseDto.Success(null, "删除成功");
    }

    public async Task<ApiResponseDto> BatchDeleteAsync(List<long> ids)
    {
        if (ids == null || ids.Count == 0)
        {
            return ApiResponseDto.Fail("请选择要删除的用户", 400);
        }

        var deletedCount = 0;
        foreach (var id in ids)
        {
            await _userRepository.DeleteAsync(id);
            deletedCount++;
        }

        return ApiResponseDto.Success(null, $"成功删除 {deletedCount} 个用户");
    }

    public async Task<ApiResponseDto> ResetPasswordAsync(long id, string newPassword)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
        {
            throw new InvalidOperationException("用户不存在");
        }

        user.PasswordHash = _passwordHasher.HashPassword(newPassword);
        await _userRepository.UpdateAsync(user);
        return ApiResponseDto.Success(null, "密码重置成功");
    }

    public async Task<ApiResponseDto> ChangePasswordAsync(long userId, string oldPassword, string newPassword)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new InvalidOperationException("用户不存在");
        }

        if (!_passwordHasher.VerifyPassword(oldPassword, user.PasswordHash))
        {
            throw new InvalidOperationException("原密码错误");
        }

        user.PasswordHash = _passwordHasher.HashPassword(newPassword);
        await _userRepository.UpdateAsync(user);
        return ApiResponseDto.Success(null, "密码修改成功");
    }

    public async Task<ApiResponseDto> AssignRolesAsync(long userId, List<long> roleIds)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new InvalidOperationException("用户不存在");
        }

        // 移除现有角色
        user.UserRoles.Clear();

        // 添加新角色
        foreach (var roleId in roleIds)
        {
            user.UserRoles.Add(new UserRole
            {
                UserId = userId,
                RoleId = roleId
            });
        }

        await _userRepository.UpdateAsync(user);
        return ApiResponseDto.Success(null, "角色分配成功");
    }

    public async Task<ApiResponseDto<List<UserRoleDto>>> GetUserRolesAsync(long userId)
    {
        var roles = await _roleRepository.GetByUserIdAsync(userId);
        var result = roles.Select(r => new UserRoleDto
        {
            Id = r.Id,
            Name = r.Name,
            Code = r.Code
        }).ToList();
        return ApiResponseDto<List<UserRoleDto>>.Success(result);
    }

    private UserDto MapToDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            UserName = user.UserName,
            RealName = user.RealName,
            Email = user.Email,
            Phone = user.Phone,
            Avatar = user.Avatar,
            Status = user.Status,
            LastLoginTime = user.LastLoginTime,
            LastLoginIp = user.LastLoginIp,
            OrganizationId = user.OrganizationId,
            TenantId = user.TenantId,
            CreatedTime = user.CreatedTime,
            UpdatedTime = user.UpdatedTime
        };
    }
}

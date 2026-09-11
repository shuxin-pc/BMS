using Bms.BuildingBlocks.Abstractions.Security;
using Bms.BuildingBlocks.Core.Search;
using Bms.System.Application.Dtos;
using Bms.System.Domain.Enums;
using Bms.System.Domain.Interfaces;
using Bms.System.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Bms.System.Application.Services;

/// <summary>
/// 全局搜索应用服务实现（系统业务数据）
/// 用户分组按用户名/真实姓名/手机号模糊匹配（忽略大小写，跟随 UserRepository 惯例）；
/// 租户与数据权限隔离复用用户列表页的判断模式（超管不限，非超管限本租户，
/// 普通用户额外按 CreatorTenantId 与 DataScope 过滤），不抽公共类。
/// </summary>
public class GlobalSearchAppService : IGlobalSearchAppService
{
    private readonly SystemDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDataPermissionFilter _dataPermissionFilter;

    public GlobalSearchAppService(
        SystemDbContext dbContext,
        ICurrentUser currentUser,
        IDataPermissionFilter dataPermissionFilter)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _dataPermissionFilter = dataPermissionFilter;
    }

    /// <inheritdoc />
    public async Task<ApiResponseDto<List<SearchResultGroupDto>>> SearchAsync(GlobalSearchRequestDto request)
    {
        var kw = request.Keyword?.Trim() ?? string.Empty;
        if (kw.Length == 0 || kw.Length > 50)
            return ApiResponseDto<List<SearchResultGroupDto>>.Fail("搜索关键字无效");
        var limit = Math.Clamp(request.Limit, 1, 10);

        var groups = new List<SearchResultGroupDto>();
        var users = await SearchUsersAsync(kw, limit);
        if (users.Count > 0)
            groups.Add(new SearchResultGroupDto { Group = "用户", Items = users });

        return ApiResponseDto<List<SearchResultGroupDto>>.Success(groups);
    }

    /// <summary>
    /// 用户分组：按用户名/真实姓名/手机号模糊匹配，复用用户列表页的租户与数据权限隔离逻辑
    /// </summary>
    private async Task<List<SearchResultItemDto>> SearchUsersAsync(string kw, int limit)
    {
        var kwLower = kw.ToLower();
        var query = _dbContext.Users
            .Where(u => !u.IsDeleted)
            .Where(u => u.UserName.ToLower().Contains(kwLower) ||
                        u.RealName.ToLower().Contains(kwLower) ||
                        (u.Phone != null && u.Phone.ToLower().Contains(kwLower)));

        // 租户隔离：非超管只能看到当前租户的用户（与 UsersController.GetList 判断模式一致）
        if (!_currentUser.IsSuperAdmin)
        {
            var tenantId = _currentUser.TenantId ?? 0;
            query = query.Where(u => u.TenantId == tenantId);

            // tenant_admin 由 super_admin 创建（CreatorTenantId=平台租户），不应被过滤掉；
            // 普通用户额外按 CreatorTenantId 过滤：屏蔽平台跨租户创建的用户（含 tenant_admin）
            if (!_currentUser.IsTenantAdmin)
            {
                query = query.Where(u => u.CreatorTenantId == tenantId);
            }

            // 数据权限过滤：非超管按 DataScope 过滤
            var userId = _currentUser.UserId ?? 0;
            var scope = await _dataPermissionFilter.GetDataPermissionScopeAsync(userId);
            if (scope.ScopeType == DataScopeType.Self)
            {
                query = query.Where(u => u.Id == userId);
            }
            else if (scope.ScopeType == DataScopeType.DepartmentAndBelow || scope.ScopeType == DataScopeType.Custom)
            {
                query = query.Where(u => u.OrganizationId != null && scope.OrganizationIds.Contains(u.OrganizationId.Value));
            }
        }

        var rows = await query
            .OrderBy(u => u.RealName)
            .Take(limit)
            .Select(u => new { u.UserName, u.RealName, u.Phone })
            .ToListAsync();

        return rows.Select(u => new SearchResultItemDto
        {
            Title = string.IsNullOrEmpty(u.RealName) ? u.UserName : u.RealName,
            Subtitle = string.IsNullOrEmpty(u.Phone) ? u.UserName : $"{u.UserName} · {u.Phone}"
        }).ToList();
    }
}

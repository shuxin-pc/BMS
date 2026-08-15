using Bms.BuildingBlocks.Abstractions.Security;
using Bms.BuildingBlocks.MultiTenant.Abstractions;
using Bms.System.Application.Dtos;
using Bms.System.Application.Dtos.Organizations;
using Bms.System.Domain.Entities;
using Bms.System.Domain.Enums;
using Bms.System.Domain.Exceptions;
using Bms.System.Domain.Interfaces;
using Bms.System.Domain.IRepositories;

namespace Bms.System.Application.Services;

public class OrganizationAppService : IOrganizationAppService
{
    private readonly IOrganizationRepository _organizationRepository;
    private readonly ITenantStore _tenantStore;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUser _currentUser;
    private readonly IDataPermissionFilter _dataPermissionFilter;

    public OrganizationAppService(
        IOrganizationRepository organizationRepository,
        ITenantStore tenantStore,
        IUserRepository userRepository,
        ICurrentUser currentUser,
        IDataPermissionFilter dataPermissionFilter)
    {
        _organizationRepository = organizationRepository;
        _tenantStore = tenantStore;
        _userRepository = userRepository;
        _currentUser = currentUser;
        _dataPermissionFilter = dataPermissionFilter;
    }

    /// <summary>
    /// 获取非超级管理员的数据权限范围。
    /// super_admin 返回 null 表示放行。
    /// 其他用户（含 tenant_admin）返回数据权限范围：
    /// - Self 直接拒绝（组织管理属于组织级操作，不包含在"仅本人"数据权限内）
    /// - All 放行，不依赖 OrganizationIds 是否非空（覆盖租户冷启动场景：本租户下还没有任何组织时，
    ///   DataPermissionFilter 不会填充 OrganizationIds，但 All 等价于本租户全量组织，租户隔离由 effectiveTenantId 保障）
    /// - DeptAndBelow/Custom 必须有具体组织 ID 列表，否则拒绝
    /// </summary>
    private async Task<DataPermissionScope?> GetNormalUserDataScopeAsync()
    {
        if (_currentUser.IsSuperAdmin)
        {
            return null;
        }

        var userId = _currentUser.UserId
            ?? throw new PermissionDeniedException("无法获取当前用户信息");
        var scope = await _dataPermissionFilter.GetDataPermissionScopeAsync(userId);
        if (scope.ScopeType == DataScopeType.Self)
        {
            throw new PermissionDeniedException("数据权限为仅本人，无权管理组织架构");
        }
        if (scope.ScopeType != DataScopeType.All && !scope.OrganizationIds.Any())
        {
            throw new PermissionDeniedException("数据权限范围内无组织，无权管理组织架构");
        }
        return scope;
    }

    /// <summary>
    /// 校验目标组织在数据权限范围内（普通用户场景）。
    /// 防止低组织用户越权管理高层级组织。
    /// All 范围直接放行（等价于本租户全量组织，租户隔离由 effectiveTenantId 保障）。
    /// </summary>
    private void CheckOrganizationInScope(Organization targetOrg, DataPermissionScope scope)
    {
        if (scope.ScopeType == DataScopeType.All)
        {
            return;
        }
        if (!scope.OrganizationIds.Contains(targetOrg.Id))
        {
            throw new PermissionDeniedException("目标组织不在你的数据权限范围内");
        }
    }

    /// <summary>
    /// 校验创建场景的父组织权限（普通用户场景）。
    /// 普通用户不能创建顶级组织（无父组织），且父组织必须在数据权限范围内。
    /// All 范围直接放行（允许创建顶级组织，租户隔离由 effectiveTenantId 保障）。
    /// </summary>
    private void CheckParentInScopeForCreate(long? parentId, DataPermissionScope scope)
    {
        if (scope.ScopeType == DataScopeType.All)
        {
            return;
        }
        if (!parentId.HasValue || parentId.Value <= 0)
        {
            throw new PermissionDeniedException("无权创建顶级组织，仅管理员可操作");
        }
        if (!scope.OrganizationIds.Contains(parentId.Value))
        {
            throw new PermissionDeniedException("父组织不在你的数据权限范围内");
        }
    }

    /// <summary>
    /// 校验更新场景的父组织权限（普通用户场景）。
    /// - parentId 未改变（0 与 null 均视为顶级）：不校验（允许修改名称等非层级属性）。
    /// - 改为顶级：拒绝（普通用户不能将组织移动为顶级）。
    /// - 改为非顶级：新父组织必须在数据权限范围内。
    /// All 范围直接放行（允许任意层级变更，租户隔离由 effectiveTenantId 保障）。
    /// </summary>
    private void CheckParentInScopeForUpdate(long? newParentId, long? originalParentId, DataPermissionScope scope)
    {
        if (scope.ScopeType == DataScopeType.All)
        {
            return;
        }

        // 统一将 0/null 视为顶级组织，避免前端传 0 而库中存 null 导致误判层级变更
        var normalizedNew = (newParentId.HasValue && newParentId.Value > 0) ? newParentId : null;
        var normalizedOriginal = (originalParentId.HasValue && originalParentId.Value > 0) ? originalParentId : null;

        if (normalizedNew == normalizedOriginal)
        {
            return;
        }

        if (normalizedNew == null)
        {
            throw new PermissionDeniedException("无权将组织移动为顶级组织，仅管理员可操作");
        }
        if (!scope.OrganizationIds.Contains(normalizedNew.Value))
        {
            throw new PermissionDeniedException("父组织不在你的数据权限范围内");
        }
    }

    /// <summary>
    /// 校验当前用户能否查询目标组织详情/子组织
    /// 修复 H2：组织详情/子组织信息泄露
    /// 查询场景校验：super_admin 放行 + 数据权限范围校验（覆盖租户隔离）
    /// All 放行（覆盖租户冷启动场景，租户隔离由 effectiveTenantId 保障）
    /// DeptAndBelow/Custom 必须有具体组织 ID 列表且目标组织在范围内
    /// Self 直接拒绝
    /// 注意：CreateAsync/UpdateAsync 内部调用 GetByIdAsync 时，已通过更严格的
    /// CheckOrganizationInScope 校验，此处校验不会阻断
    /// </summary>
    private async Task CheckCanReadOrganizationAsync(Organization targetOrg)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId == null)
        {
            throw new PermissionDeniedException("无法识别当前用户身份");
        }

        // super_admin 放行
        if (_currentUser.IsSuperAdmin)
        {
            return;
        }

        // 非超级管理员：基于数据权限范围校验
        var userId = _currentUser.UserId.Value;
        var scope = await _dataPermissionFilter.GetDataPermissionScopeAsync(userId);
        if (scope.ScopeType == DataScopeType.Self)
        {
            throw new PermissionDeniedException("数据权限为仅本人，无权查看组织信息");
        }
        if (scope.ScopeType != DataScopeType.All)
        {
            if (!scope.OrganizationIds.Any())
            {
                throw new PermissionDeniedException("数据权限范围内无组织，无权查看组织信息");
            }
            if (!scope.OrganizationIds.Contains(targetOrg.Id))
            {
                throw new PermissionDeniedException("目标组织不在你的数据权限范围内");
            }
        }
    }

    /// <summary>
    /// 过滤组织列表：数据权限范围过滤（覆盖租户隔离）
    /// 修复 H2：GetChildren 场景批量过滤，避免逐条调用 CheckCanReadOrganizationAsync 产生多次数据权限查询
    /// All 直接返回全部（覆盖租户冷启动场景，租户隔离由 effectiveTenantId 保障）
    /// DeptAndBelow/Custom 按 OrganizationIds 过滤；Self 返回空
    /// </summary>
    private async Task<List<Organization>> FilterOrganizationsByReadPermissionAsync(List<Organization> organizations)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId == null)
        {
            return new List<Organization>();
        }

        // super_admin 放行
        if (_currentUser.IsSuperAdmin)
        {
            return organizations;
        }

        // 非超级管理员：基于数据权限范围过滤
        var userId = _currentUser.UserId.Value;
        var scope = await _dataPermissionFilter.GetDataPermissionScopeAsync(userId);
        if (scope.ScopeType == DataScopeType.All)
        {
            return organizations;
        }
        if (scope.ScopeType == DataScopeType.Self || !scope.OrganizationIds.Any())
        {
            return new List<Organization>();
        }
        var scopeOrgIds = scope.OrganizationIds.ToHashSet();
        return organizations.Where(o => scopeOrgIds.Contains(o.Id)).ToList();
    }

    public async Task<List<OrganizationDto>> GetTreeListAsync(OrganizationQueryDto? query, bool isSuperAdmin = true, long? tenantId = null)
    {
        var organizations = await _organizationRepository.GetAllTreeAsync();

        // 租户隔离：按租户筛选
        if (tenantId.HasValue)
        {
            organizations = FilterByTenant(organizations, tenantId.Value);
        }

        // 如果有筛选条件，检查是否有匹配结果
        if (query != null && (!string.IsNullOrEmpty(query.Name) || query.Status.HasValue))
        {
            var hasMatch = CheckHasMatch(organizations, query);

            // 如果没有匹配结果，返回空数组
            if (!hasMatch)
            {
                return new List<OrganizationDto>();
            }
        }

        // 返回完整树形，并标记匹配节点
        return await MapToTreeDtoWithMatchAsync(organizations, query);
    }

    /// <summary>
    /// 检查是否有匹配结果
    /// </summary>
    private bool CheckHasMatch(List<Organization> organizations, OrganizationQueryDto query)
    {
        foreach (var org in organizations)
        {
            if (IsOrganizationMatch(org, query))
                return true;
            if (org.Children.Any() && CheckHasMatch(org.Children.ToList(), query))
                return true;
        }
        return false;
    }

    /// <summary>
    /// 映射到DTO并标记匹配节点（异步）
    /// </summary>
    private async Task<List<OrganizationDto>> MapToTreeDtoWithMatchAsync(List<Organization> organizations, OrganizationQueryDto? query)
    {
        // 顺序执行，避免并发使用同一个DbContext
        var result = new List<OrganizationDto>();
        foreach (var org in organizations)
        {
            result.Add(await MapToDtoWithMatchAsync(org, query));
        }
        return result;
    }

    private async Task<OrganizationDto> MapToDtoWithMatchAsync(Organization org, OrganizationQueryDto? query)
    {
        // 查询负责人姓名
        string? managerName = null;
        if (org.ManagerId.HasValue && org.ManagerId.Value > 0)
        {
            var manager = await _userRepository.GetByIdAsync(org.ManagerId.Value);
            managerName = manager?.RealName;
        }

        var dto = new OrganizationDto
        {
            Id = org.Id,
            ParentId = org.ParentId,
            Name = org.Name,
            Code = org.Code,
            Type = org.Type,
            ManagerId = org.ManagerId,
            ManagerName = managerName,
            Phone = org.Phone,
            Address = org.Address,
            Status = org.Status,
            Sort = org.Sort,
            TenantId = org.TenantId,
            CreatedTime = org.CreatedTime,
            UpdatedTime = org.UpdatedTime,
            // 标记是否匹配搜索条件
            IsMatched = query != null && IsOrganizationMatch(org, query)
        };

        // 映射子组织（顺序执行）
        if (org.Children != null && org.Children.Any())
        {
            var children = new List<OrganizationDto>();
            foreach (var child in org.Children)
            {
                children.Add(await MapToDtoWithMatchAsync(child, query));
            }
            dto.Children = children;
        }

        return dto;
    }

    /// <summary>
    /// 检查组织是否匹配查询条件
    /// </summary>
    private bool IsOrganizationMatch(Organization org, OrganizationQueryDto query)
    {
        // 名称过滤（忽略大小写）
        if (!string.IsNullOrEmpty(query.Name) && !org.Name.ToLower().Contains(query.Name.ToLower()))
            return false;
        // 状态过滤
        if (query.Status.HasValue && org.Status != query.Status.Value)
            return false;
        return true;
    }

    public async Task<List<OrganizationDto>> GetListAsync(OrganizationQueryDto query, bool isSuperAdmin = true, long? tenantId = null)
    {
        var organizations = await _organizationRepository.GetListAsync();

        // 租户隔离：按租户筛选
        if (tenantId.HasValue)
        {
            organizations = organizations.Where(o => o.TenantId == tenantId.Value).ToList();
        }

        // 过滤（忽略大小写）
        if (!string.IsNullOrEmpty(query.Name))
        {
            organizations = organizations.Where(o => o.Name.ToLower().Contains(query.Name.ToLower())).ToList();
        }

        if (!string.IsNullOrEmpty(query.Type))
        {
            organizations = organizations.Where(o => o.Type.ToString() == query.Type).ToList();
        }

        if (query.Status.HasValue)
        {
            organizations = organizations.Where(o => o.Status == query.Status.Value).ToList();
        }

        // 顺序执行，避免并发使用同一个DbContext
        var result = new List<OrganizationDto>();
        foreach (var org in organizations)
        {
            result.Add(await MapToDtoAsync(org));
        }
        return result;
    }

    /// <summary>
    /// 递归过滤租户组织
    /// </summary>
    private List<Organization> FilterByTenant(List<Organization> organizations, long tenantId)
    {
        return organizations
            .Where(o => o.TenantId == tenantId)
            .Select(o =>
            {
                if (o.Children.Any())
                {
                    o.Children = new List<Organization>(FilterByTenant(o.Children.ToList(), tenantId));
                }
                return o;
            })
            .ToList();
    }

    public async Task<OrganizationDto?> GetByIdAsync(long id)
    {
        var organization = await _organizationRepository.GetByIdAsync(id);
        if (organization == null)
        {
            return null;
        }

        // 修复 H2：校验当前用户能否查询此组织（租户隔离 + 数据权限范围）
        await CheckCanReadOrganizationAsync(organization);

        return await MapToDtoAsync(organization);
    }

    public async Task<List<OrganizationDto>> GetChildrenAsync(long? parentId)
    {
        var organizations = await _organizationRepository.GetChildrenAsync(parentId);

        // 修复 H2：租户隔离 + 数据权限范围过滤，防止跨租户和越权查看子组织
        organizations = await FilterOrganizationsByReadPermissionAsync(organizations);

        // 顺序执行，避免并发使用同一个DbContext
        var result = new List<OrganizationDto>();
        foreach (var org in organizations)
        {
            result.Add(await MapToDtoAsync(org));
        }
        return result;
    }

    public async Task<OrganizationDto> CreateAsync(OrganizationCreateDto dto)
    {
        // 权限校验：非超级管理员基于数据权限范围（super_admin 放行）
        var dataScope = await GetNormalUserDataScopeAsync();

        if (await _organizationRepository.ExistsCodeAsync(dto.Code))
        {
            throw new InvalidOperationException($"组织编码 {dto.Code} 已存在");
        }

        // 非 super_admin 强制使用当前租户（防止前端伪造租户ID跨租户创建组织）
        var currentTenantId = _currentUser.TenantId ?? 0;
        var effectiveTenantIdStr = _currentUser.IsSuperAdmin ? dto.TenantId : currentTenantId.ToString();
        if (!long.TryParse(effectiveTenantIdStr, out long tenantIdLong))
        {
            tenantIdLong = 0;
        }

        // 验证父组织是否存在（parentId 为 0 或 null 表示顶级组织，不需要验证）
        if (dto.ParentId.HasValue && dto.ParentId.Value > 0)
        {
            var parent = await _organizationRepository.GetByIdAsync(dto.ParentId.Value);
            if (parent == null)
            {
                throw new InvalidOperationException($"父组织 {dto.ParentId} 不存在");
            }
            // 父组织租户校验由 CheckParentInScopeForCreate 统一处理（基于 DataScope）
        }

        // 非超级管理员：父组织必须在数据权限范围内，且不能创建顶级组织
        if (dataScope != null)
        {
            CheckParentInScopeForCreate(dto.ParentId, dataScope);
        }

        // 获取租户编码（冗余字段）
        var tenantCode = string.Empty;
        if (tenantIdLong > 0)
        {
            var tenant = await _tenantStore.GetTenantByIdAsync(tenantIdLong);
            tenantCode = tenant?.Code ?? string.Empty;
        }

        // 将 parentId 为 0 转换为 null，表示顶级组织
        var parentId = dto.ParentId.HasValue && dto.ParentId.Value > 0 ? dto.ParentId : null;

        var organization = new Organization
        {
            ParentId = parentId,
            Name = dto.Name,
            Code = dto.Code,
            Type = dto.Type,
            ManagerId = dto.ManagerId,
            Phone = dto.Phone,
            Address = dto.Address,
            Status = dto.Status,
            Sort = dto.Sort,
            TenantId = tenantIdLong,
            TenantCode = tenantCode
        };

        await _organizationRepository.AddAsync(organization);
        // 修复 H2：直接映射返回，不调用 GetByIdAsync（会触发 CheckCanReadOrganizationAsync）
        // 新建组织的 Id 还不在操作者数据权限范围 OrganizationIds 内，调用 GetByIdAsync 会被误拒
        return await MapToDtoAsync(organization);
    }

    public async Task<OrganizationDto> UpdateAsync(OrganizationUpdateDto dto)
    {
        // 权限校验：普通用户基于数据权限范围（super_admin/tenant_admin 放行）
        var dataScope = await GetNormalUserDataScopeAsync();

        var organization = await _organizationRepository.GetByIdAsync(dto.Id);
        if (organization == null)
        {
            throw new InvalidOperationException("组织不存在");
        }

        // 非超级管理员：目标组织必须在数据权限范围内（防止越权管理高层级组织）
        // tenant_admin 的 DataScope=All 覆盖本租户全量组织，跨租户组织会被拒绝（等价于租户隔离）
        if (dataScope != null)
        {
            CheckOrganizationInScope(organization, dataScope);
        }

        if (await _organizationRepository.ExistsCodeAsync(dto.Code, dto.Id))
        {
            throw new InvalidOperationException($"组织编码 {dto.Code} 已存在");
        }

        // 验证父组织是否存在（parentId 为 0 或 null 表示顶级组织，不需要验证）
        if (dto.ParentId.HasValue && dto.ParentId.Value > 0)
        {
            var parent = await _organizationRepository.GetByIdAsync(dto.ParentId.Value);
            if (parent == null)
            {
                throw new InvalidOperationException($"父组织 {dto.ParentId} 不存在");
            }
            // 父组织租户校验由 CheckParentInScopeForUpdate 统一处理（基于 DataScope）
        }

        // 不能设置自己为父组织
        if (dto.ParentId.HasValue && dto.ParentId.Value == dto.Id)
        {
            throw new InvalidOperationException("不能将自己设置为父组织");
        }

        // 修复 M1：不能将组织移动到自己的子孙下，否则形成环路导致递归查询栈溢出
        if (dto.ParentId.HasValue && dto.ParentId.Value > 0 && dto.ParentId.Value != dto.Id)
        {
            var descendantIds = await _organizationRepository.GetAllChildIdsAsync(dto.Id);
            if (descendantIds.Contains(dto.ParentId.Value))
            {
                throw new InvalidOperationException("不能将组织移动到其子组织下");
            }
        }

        // 非超级管理员：若改变层级，新父组织必须在数据权限范围内，且不能移动为顶级
        if (dataScope != null)
        {
            CheckParentInScopeForUpdate(dto.ParentId, organization.ParentId, dataScope);
        }

        // 将 parentId 为 0 转换为 null，表示顶级组织
        organization.ParentId = dto.ParentId.HasValue && dto.ParentId.Value > 0 ? dto.ParentId : null;
        organization.Name = dto.Name;
        organization.Code = dto.Code;
        organization.Type = dto.Type;
        organization.ManagerId = dto.ManagerId;
        organization.Phone = dto.Phone;
        organization.Address = dto.Address;
        organization.Status = dto.Status;
        organization.Sort = dto.Sort;

        await _organizationRepository.UpdateAsync(organization);
        return await GetByIdAsync(organization.Id) ?? throw new InvalidOperationException("更新组织失败");
    }

    public async Task DeleteAsync(long id)
    {
        // 权限校验：非超级管理员基于数据权限范围（super_admin 放行）
        var dataScope = await GetNormalUserDataScopeAsync();

        var organization = await _organizationRepository.GetByIdAsync(id);
        if (organization == null)
        {
            throw new InvalidOperationException("组织不存在");
        }

        // 非超级管理员：目标组织必须在数据权限范围内（防止越权删除高层级组织）
        // tenant_admin 的 DataScope=All 覆盖本租户全量组织，跨租户组织会被拒绝（等价于租户隔离）
        if (dataScope != null)
        {
            CheckOrganizationInScope(organization, dataScope);
        }

        // 检查是否有子组织
        var children = await _organizationRepository.GetChildrenAsync(id);
        if (children.Any())
        {
            throw new InvalidOperationException("存在子组织，不能删除");
        }

        // 检查是否有用户
        var hasUsers = await _organizationRepository.HasUsersAsync(id);
        if (hasUsers)
        {
            throw new InvalidOperationException("该组织下存在用户，不能删除");
        }

        await _organizationRepository.DeleteAsync(id);
    }

    private async Task<OrganizationDto> MapToDtoAsync(Organization organization)
    {
        // 查询负责人姓名
        string? managerName = null;
        if (organization.ManagerId.HasValue && organization.ManagerId.Value > 0)
        {
            var manager = await _userRepository.GetByIdAsync(organization.ManagerId.Value);
            managerName = manager?.RealName;
        }

        var dto = new OrganizationDto
        {
            Id = organization.Id,
            ParentId = organization.ParentId,
            Name = organization.Name,
            Code = organization.Code,
            Type = organization.Type,
            ManagerId = organization.ManagerId,
            ManagerName = managerName,
            Phone = organization.Phone,
            Address = organization.Address,
            Status = organization.Status,
            Sort = organization.Sort,
            TenantId = organization.TenantId,
            CreatedTime = organization.CreatedTime,
            UpdatedTime = organization.UpdatedTime
        };

        // 映射子组织（顺序执行）
        if (organization.Children != null && organization.Children.Any())
        {
            var children = new List<OrganizationDto>();
            foreach (var child in organization.Children)
            {
                children.Add(await MapToDtoAsync(child));
            }
            dto.Children = children;
        }

        return dto;
    }
}

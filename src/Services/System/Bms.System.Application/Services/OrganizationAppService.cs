using Bms.BuildingBlocks.MultiTenant.Abstractions;
using Bms.System.Application.Dtos;
using Bms.System.Application.Dtos.Organizations;
using Bms.System.Domain.Entities;
using Bms.System.Domain.IRepositories;

namespace Bms.System.Application.Services;

public class OrganizationAppService : IOrganizationAppService
{
    private readonly IOrganizationRepository _organizationRepository;
    private readonly ITenantStore _tenantStore;
    private readonly IUserRepository _userRepository;

    public OrganizationAppService(IOrganizationRepository organizationRepository, ITenantStore tenantStore, IUserRepository userRepository)
    {
        _organizationRepository = organizationRepository;
        _tenantStore = tenantStore;
        _userRepository = userRepository;
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
        return organization == null ? null : await MapToDtoAsync(organization);
    }

    public async Task<List<OrganizationDto>> GetChildrenAsync(long? parentId)
    {
        var organizations = await _organizationRepository.GetChildrenAsync(parentId);
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
        if (await _organizationRepository.ExistsCodeAsync(dto.Code))
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
        }

        // 解析租户ID（字符串转long，避免JavaScript Number精度丢失）
        if (!long.TryParse(dto.TenantId, out long tenantIdLong))
        {
            tenantIdLong = 0;
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
        return await GetByIdAsync(organization.Id) ?? throw new InvalidOperationException("创建组织失败");
    }

    public async Task<OrganizationDto> UpdateAsync(OrganizationUpdateDto dto)
    {
        var organization = await _organizationRepository.GetByIdAsync(dto.Id);
        if (organization == null)
        {
            throw new InvalidOperationException("组织不存在");
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
        }

        // 不能设置自己为父组织
        if (dto.ParentId.HasValue && dto.ParentId.Value == dto.Id)
        {
            throw new InvalidOperationException("不能将自己设置为父组织");
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

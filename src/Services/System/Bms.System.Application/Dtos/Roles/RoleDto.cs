namespace Bms.System.Application.Dtos.Roles;

public class RoleDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public long? TenantId { get; set; }
    public bool IsSystem { get; set; }
    public int Status { get; set; }
    /// <summary>
    /// 角色等级（数字越小权限越大，super_admin=0, tenant_admin=1, 普通角色>1）
    /// </summary>
    public int Level { get; set; }
    public int DataScopeType { get; set; }
    public string? CustomOrganizationIds { get; set; }
    public DateTime CreatedTime { get; set; }
    public DateTime? UpdatedTime { get; set; }
    public List<RoleMenuDto> Menus { get; set; } = new();
}

public class RoleMenuDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Path { get; set; }
    public int Type { get; set; }
}

public class RoleCreateDto
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Status { get; set; } = 1;
    /// <summary>
    /// 角色等级（2-99，数字越小权限越大；0/1 为系统保留角色，不可创建）
    /// </summary>
    public int Level { get; set; }
    public int DataScopeType { get; set; } = 1;
    public List<long> PermissionIds { get; set; } = new();
    public List<long>? CustomOrganizationIds { get; set; }
}

public class RoleUpdateDto
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Status { get; set; }
    /// <summary>
    /// 角色等级（2-99，数字越小权限越大；系统保留角色不可修改）
    /// </summary>
    public int Level { get; set; }
    public int DataScopeType { get; set; }
    public List<long> PermissionIds { get; set; } = new();
    public List<long>? CustomOrganizationIds { get; set; }
}

public class RoleAssignPermissionsDto
{
    public long RoleId { get; set; }
    public List<long> PermissionIds { get; set; } = new();
}
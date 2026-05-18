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
    public int Sort { get; set; }
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
    public int Sort { get; set; }
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
    public int Sort { get; set; }
    public int DataScopeType { get; set; }
    public List<long> PermissionIds { get; set; } = new();
    public List<long>? CustomOrganizationIds { get; set; }
}

public class RoleAssignPermissionsDto
{
    public long RoleId { get; set; }
    public List<long> PermissionIds { get; set; } = new();
}
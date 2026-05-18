namespace Bms.System.Application.Dtos.Organizations;

public class OrganizationDto
{
    public long Id { get; set; }
    public long? ParentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public int Type { get; set; }
    public long? ManagerId { get; set; }
    public string? ManagerName { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public int Status { get; set; }
    public int Sort { get; set; }
    public long TenantId { get; set; }
    public DateTime CreatedTime { get; set; }
    public DateTime? UpdatedTime { get; set; }
    public List<OrganizationDto>? Children { get; set; }
    /// <summary>
    /// 是否匹配搜索条件（仅用于前端高亮）
    /// </summary>
    public bool IsMatched { get; set; }
}

public class OrganizationCreateDto
{
    public long? ParentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public int Type { get; set; }
    public long? ManagerId { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public int Status { get; set; }
    public int Sort { get; set; }
    /// <summary>
    /// 租户ID（使用字符串接收，避免JavaScript Number精度丢失）
    /// </summary>
    public string TenantId { get; set; } = string.Empty;
}

public class OrganizationUpdateDto
{
    public long Id { get; set; }
    public long? ParentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public int Type { get; set; }
    public long? ManagerId { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public int Status { get; set; }
    public int Sort { get; set; }
}

public class OrganizationQueryDto
{
    public string? Name { get; set; }
    public string? Type { get; set; }
    public int? Status { get; set; }
    public long? TenantId { get; set; }
}

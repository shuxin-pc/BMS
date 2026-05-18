namespace Bms.System.Application.Dtos.SystemConfigs;

public class SystemConfigDto
{
    public long Id { get; set; }
    public long TenantId { get; set; }
    public string TenantCode { get; set; } = string.Empty;
    public string ConfigKey { get; set; } = string.Empty;
    public string? ConfigValue { get; set; }
    public string? ConfigGroup { get; set; }
    public string? Description { get; set; }
    public bool IsPublic { get; set; }
    public int Sort { get; set; }
    public bool IsEditable { get; set; }
    public bool IsSystem { get; set; }
    public DateTime CreatedTime { get; set; }
    public DateTime? UpdatedTime { get; set; }
}

public class SystemConfigCreateDto
{
    public string ConfigKey { get; set; } = string.Empty;
    public string? ConfigValue { get; set; }
    public string? ConfigGroup { get; set; }
    public string? Description { get; set; }
    public bool IsPublic { get; set; }
    public int Sort { get; set; }
    public bool IsEditable { get; set; } = true;
}

public class SystemConfigUpdateDto
{
    public long Id { get; set; }
    public string? ConfigValue { get; set; }
    public string? ConfigGroup { get; set; }
    public string? Description { get; set; }
    public bool IsPublic { get; set; }
    public bool IsEditable { get; set; }
    public int Sort { get; set; }
}

public class SystemConfigQueryDto
{
    public string? ConfigKey { get; set; }
    public string? ConfigGroup { get; set; }
    public long? TenantId { get; set; }
}

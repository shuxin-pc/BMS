namespace Bms.System.Application.Dtos.Permissions;

public class PermissionDto
{
    public long Id { get; set; }
    public long MenuId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? HttpMethod { get; set; }
    public string? ApiPath { get; set; }
    public DateTime CreatedTime { get; set; }
}

public class PermissionCreateDto
{
    public long MenuId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? HttpMethod { get; set; }
    public string? ApiPath { get; set; }
}

public class PermissionUpdateDto
{
    public long Id { get; set; }
    public long MenuId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? HttpMethod { get; set; }
    public string? ApiPath { get; set; }
}

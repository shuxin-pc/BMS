using Bms.System.Domain.Enums;

namespace Bms.System.Application.Dtos.DataPermissions;

public class DataPermissionDto
{
    public long Id { get; set; }
    public long RoleId { get; set; }
    public DataScopeType DataScopeType { get; set; }
    public string? CustomOrganizationIds { get; set; }
    public DateTime CreatedTime { get; set; }
    public DateTime? UpdatedTime { get; set; }
}

public class DataPermissionCreateDto
{
    public long RoleId { get; set; }
    public DataScopeType DataScopeType { get; set; }
    public string? CustomOrganizationIds { get; set; }
}

public class DataPermissionUpdateDto
{
    public long Id { get; set; }
    public DataScopeType DataScopeType { get; set; }
    public string? CustomOrganizationIds { get; set; }
}

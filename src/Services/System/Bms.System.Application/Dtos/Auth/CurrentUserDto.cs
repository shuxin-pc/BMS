namespace Bms.System.Application.Dtos.Auth;

/// <summary>
/// 当前用户信息DTO
/// </summary>
public class CurrentUserDto
{
    public long Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? RealName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Avatar { get; set; }
    public long TenantId { get; set; }
    public string? TenantCode { get; set; }
    public List<string> Roles { get; set; } = new();
    public List<long> RoleIds { get; set; } = new();
    public List<string> Permissions { get; set; } = new();
}

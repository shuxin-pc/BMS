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

    /// <summary>
    /// 当前用户最高角色等级（数字越小权限越大，无角色时为 100）
    /// 用于前端动态限制可创建/编辑的角色等级范围
    /// </summary>
    public int MaxRoleLevel { get; set; }
}

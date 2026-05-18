namespace Bms.System.Application.Dtos.Profile;

public class ProfileDto
{
    public long Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string RealName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Avatar { get; set; }
    public DateTime? LastLoginTime { get; set; }
    public string? LastLoginIp { get; set; }
    public DateTime CreatedTime { get; set; }
    /// <summary>
    /// 所属组织名称
    /// </summary>
    public string? OrganizationName { get; set; }
    /// <summary>
    /// 所属租户名称
    /// </summary>
    public string? TenantName { get; set; }
    /// <summary>
    /// 角色列表
    /// </summary>
    public List<ProfileRoleDto> Roles { get; set; } = new();
}

/// <summary>
/// 个人中心角色信息
/// </summary>
public class ProfileRoleDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class UpdateProfileDto
{
    public string? RealName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
}

public class ChangePasswordDto
{
    public string OldPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

public class UpdateAvatarDto
{
    public string Avatar { get; set; } = string.Empty;
}

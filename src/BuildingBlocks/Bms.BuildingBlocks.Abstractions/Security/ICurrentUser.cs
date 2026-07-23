namespace Bms.BuildingBlocks.Abstractions.Security;

/// <summary>
/// 当前用户信息接口
/// </summary>
public interface ICurrentUser
{
    /// <summary>
    /// 用户ID
    /// </summary>
    long? UserId { get; }

    /// <summary>
    /// 用户名
    /// </summary>
    string? UserName { get; }

    /// <summary>
    /// 真实姓名
    /// </summary>
    string? RealName { get; }

    /// <summary>
    /// 租户ID
    /// </summary>
    long? TenantId { get; }

    /// <summary>
    /// 租户编码
    /// </summary>
    string? TenantCode { get; }

    /// <summary>
    /// 当前门店ID（从 X-Store-Id 请求头解析，仅 Store 服务 API 可用）
    /// </summary>
    long? StoreId { get; }

    /// <summary>
    /// 是否超级管理员
    /// </summary>
    bool IsSuperAdmin { get; }

    /// <summary>
    /// 是否租户管理员（角色 Code 含 tenant_admin）
    /// </summary>
    bool IsTenantAdmin { get; }

    /// <summary>
    /// 用户角色列表
    /// </summary>
    IReadOnlyList<string> Roles { get; }

    /// <summary>
    /// 是否已认证
    /// </summary>
    bool IsAuthenticated { get; }
}

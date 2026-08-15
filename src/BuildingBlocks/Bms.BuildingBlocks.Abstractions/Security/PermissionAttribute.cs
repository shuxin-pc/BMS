namespace Bms.BuildingBlocks.Abstractions.Security;

/// <summary>
/// 权限特性，用于标记API接口所需的权限码
/// 通过 PermissionMiddleware 校验：用户角色 -> RoleMenuAuths -> Menu.PermissionCode
/// super_admin/tenant_admin 自动放行（系统设计，等同拥有所有权限）
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class PermissionAttribute : Attribute
{
    /// <summary>
    /// 权限编码
    /// </summary>
    public string PermissionCode { get; set; }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="permissionCode">权限编码</param>
    public PermissionAttribute(string permissionCode)
    {
        PermissionCode = permissionCode;
    }
}

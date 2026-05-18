namespace Bms.System.Domain.Attributes;

/// <summary>
/// 权限特性，用于标记API接口所需的权限
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

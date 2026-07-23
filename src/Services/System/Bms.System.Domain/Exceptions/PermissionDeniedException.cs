namespace Bms.System.Domain.Exceptions;

/// <summary>
/// 权限拒绝异常
/// 用于业务层校验权限失败时抛出，Controller 捕获后返回 403 Forbidden
/// </summary>
public class PermissionDeniedException : Exception
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public PermissionDeniedException(string message) : base(message)
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    public PermissionDeniedException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

namespace Bms.System.Domain.Entities;

/// <summary>
/// 审计日志实体
/// </summary>
public class AuditLog : EntityBase
{
    /// <summary>
    /// 租户ID
    /// </summary>
    public long? TenantId { get; set; }

    /// <summary>
    /// 用户ID
    /// </summary>
    public long? UserId { get; set; }

    /// <summary>
    /// 用户名
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// 真实姓名
    /// </summary>
    public string? RealName { get; set; }

    /// <summary>
    /// 操作类型（登录、登出、增删改查等）
    /// </summary>
    public string OperationType { get; set; } = string.Empty;

    /// <summary>
    /// 操作内容
    /// </summary>
    public string? OperationContent { get; set; }

    /// <summary>
    /// 请求路径
    /// </summary>
    public string? RequestPath { get; set; }

    /// <summary>
    /// 请求方法
    /// </summary>
    public string? RequestMethod { get; set; }

    /// <summary>
    /// 请求IP
    /// </summary>
    public string? RequestIp { get; set; }

    /// <summary>
    /// 用户代理
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// 响应状态码
    /// </summary>
    public int? ResponseStatus { get; set; }

    /// <summary>
    /// 请求耗时（毫秒）
    /// </summary>
    public long? Duration { get; set; }

    /// <summary>
    /// 错误信息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 实体变更内容
    /// Create: 新增的完整数据
    /// Update: 变更字段详情（含新旧值）
    /// Delete: 删除前的完整数据
    /// </summary>
    public string? EntityChanges { get; set; }
}
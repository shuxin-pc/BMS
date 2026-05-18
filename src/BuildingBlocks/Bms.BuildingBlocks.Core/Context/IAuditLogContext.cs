namespace Bms.BuildingBlocks.Core.Context;

/// <summary>
/// 审计日志上下文接口
/// 用于在请求作用域内存储审计日志相关信息
/// </summary>
public interface IAuditLogContext
{
    /// <summary>
    /// 当前用户ID
    /// </summary>
    long? UserId { get; set; }

    /// <summary>
    /// 用户名
    /// </summary>
    string? UserName { get; set; }

    /// <summary>
    /// 真实姓名
    /// </summary>
    string? RealName { get; set; }

    /// <summary>
    /// 租户ID
    /// </summary>
    long? TenantId { get; set; }

    /// <summary>
    /// 请求路径
    /// </summary>
    string? RequestPath { get; set; }

    /// <summary>
    /// 请求方法
    /// </summary>
    string? RequestMethod { get; set; }

    /// <summary>
    /// 请求IP
    /// </summary>
    string? RequestIp { get; set; }

    /// <summary>
    /// 用户代理
    /// </summary>
    string? UserAgent { get; set; }

    /// <summary>
    /// 响应状态码
    /// </summary>
    int? ResponseStatus { get; set; }

    /// <summary>
    /// 请求开始时间
    /// </summary>
    DateTime? RequestTime { get; set; }

    /// <summary>
    /// 请求耗时（毫秒）
    /// </summary>
    long? Duration { get; set; }

    /// <summary>
    /// 错误信息
    /// </summary>
    string? ErrorMessage { get; set; }

    /// <summary>
    /// 是否启用审计日志
    /// </summary>
    bool IsEnabled { get; set; }

    /// <summary>
    /// 自定义操作类型（如登录、登出等），覆盖拦截器自动判断的操作类型
    /// </summary>
    string? CustomOperationType { get; set; }

    /// <summary>
    /// 待写入的审计日志列表（拦截器添加，中间件 OnCompleted 统一写入）
    /// </summary>
    IList<AuditLogEntry> PendingAuditLogs { get; }

    /// <summary>
    /// 添加待写入的审计日志
    /// </summary>
    void AddPendingAuditLog(AuditLogEntry auditLog);

    /// <summary>
    /// 清空待写入的审计日志
    /// </summary>
    void ClearPendingAuditLogs();
}

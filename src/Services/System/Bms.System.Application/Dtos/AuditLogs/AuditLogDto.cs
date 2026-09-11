namespace Bms.System.Application.Dtos.AuditLogs;

public class AuditLogDto
{
    public long Id { get; set; }
    public long? TenantId { get; set; }
    public long? StoreId { get; set; }
    public string? StoreName { get; set; }
    public long? UserId { get; set; }
    public string? UserName { get; set; }
    public string? RealName { get; set; }
    public string OperationType { get; set; } = string.Empty;
    public string? OperationContent { get; set; }
    /// <summary>
    /// 被操作对象的ID（业务实体变更时填充，自定义操作为空）
    /// </summary>
    public long? EntityId { get; set; }
    public string? RequestPath { get; set; }
    public string? RequestMethod { get; set; }
    public string? RequestIp { get; set; }
    public string? UserAgent { get; set; }
    public int? ResponseStatus { get; set; }
    public long? Duration { get; set; }
    public string? ErrorMessage { get; set; }
    /// <summary>
    /// 实体变更内容
    /// </summary>
    public string? EntityChanges { get; set; }
    public DateTime CreatedTime { get; set; }
}

public class AuditLogQueryDto
{
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? UserName { get; set; }
    public string? OperationType { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    /// <summary>
    /// 响应状态：success表示2xx，error表示4xx及以上
    /// </summary>
    public string? ResponseStatus { get; set; }
    /// <summary>
    /// 租户ID（超级管理员可筛选）
    /// </summary>
    public long? TenantId { get; set; }
}

/// <summary>
/// 创建审计日志请求DTO
/// </summary>
public class CreateAuditLogDto
{
    /// <summary>
    /// 操作类型：Login、Logout 等
    /// </summary>
    public string OperationType { get; set; } = string.Empty;

    /// <summary>
    /// 用户ID
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// 用户名
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// 真实姓名
    /// </summary>
    public string? RealName { get; set; }

    /// <summary>
    /// 租户ID
    /// </summary>
    public long? TenantId { get; set; }

    /// <summary>
    /// 门店ID（仅 Store 服务产生的日志有值）
    /// </summary>
    public long? StoreId { get; set; }

    /// <summary>
    /// 门店名称
    /// </summary>
    public string? StoreName { get; set; }

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
    public int ResponseStatus { get; set; }

    /// <summary>
    /// 请求路径
    /// </summary>
    public string? RequestPath { get; set; }
}

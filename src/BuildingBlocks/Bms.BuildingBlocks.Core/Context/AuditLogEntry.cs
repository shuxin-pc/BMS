namespace Bms.BuildingBlocks.Core.Context;

/// <summary>
/// 审计日志条目（用于在拦截器和中间件之间传递数据）
/// </summary>
public class AuditLogEntry
{
    public long? TenantId { get; set; }
    public long? UserId { get; set; }
    public string? UserName { get; set; }
    public string? RealName { get; set; }
    public string? OperationType { get; set; }
    public string? OperationContent { get; set; }
    public string? RequestPath { get; set; }
    public string? RequestMethod { get; set; }
    public string? RequestIp { get; set; }
    public string? UserAgent { get; set; }
    public int? ResponseStatus { get; set; }
    public long? Duration { get; set; }
    /// <summary>
    /// 实体变更内容
    /// Create: 新增的完整数据
    /// Update: 变更字段详情（含新旧值）
    /// Delete: 删除前的完整数据
    /// </summary>
    public string? EntityChanges { get; set; }
    public DateTime? CreatedTime { get; set; }
}

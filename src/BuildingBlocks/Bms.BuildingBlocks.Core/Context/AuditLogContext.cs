namespace Bms.BuildingBlocks.Core.Context;

/// <summary>
/// 审计日志上下文实现
/// 使用 AsyncLocal 实现请求作用域内的数据隔离
/// </summary>
public class AuditLogContext : IAuditLogContext
{
    private static readonly AsyncLocal<AuditLogContextHolder> _current = new();

    public long? UserId
    {
        get => _current.Value?.Context?.UserId;
        set
        {
            EnsureContext().UserId = value;
        }
    }

    public string? UserName
    {
        get => _current.Value?.Context?.UserName;
        set
        {
            EnsureContext().UserName = value;
        }
    }

    public string? RealName
    {
        get => _current.Value?.Context?.RealName;
        set
        {
            EnsureContext().RealName = value;
        }
    }

    public long? TenantId
    {
        get => _current.Value?.Context?.TenantId;
        set
        {
            EnsureContext().TenantId = value;
        }
    }

    public long? StoreId
    {
        get => _current.Value?.Context?.StoreId;
        set
        {
            EnsureContext().StoreId = value;
        }
    }

    public string? StoreName
    {
        get => _current.Value?.Context?.StoreName;
        set
        {
            EnsureContext().StoreName = value;
        }
    }

    public string? RequestPath
    {
        get => _current.Value?.Context?.RequestPath;
        set
        {
            EnsureContext().RequestPath = value;
        }
    }

    public string? RequestMethod
    {
        get => _current.Value?.Context?.RequestMethod;
        set
        {
            EnsureContext().RequestMethod = value;
        }
    }

    public string? RequestIp
    {
        get => _current.Value?.Context?.RequestIp;
        set
        {
            EnsureContext().RequestIp = value;
        }
    }

    public string? UserAgent
    {
        get => _current.Value?.Context?.UserAgent;
        set
        {
            EnsureContext().UserAgent = value;
        }
    }

    public int? ResponseStatus
    {
        get => _current.Value?.Context?.ResponseStatus;
        set
        {
            EnsureContext().ResponseStatus = value;
        }
    }

    public DateTime? RequestTime
    {
        get => _current.Value?.Context?.RequestTime;
        set
        {
            EnsureContext().RequestTime = value;
        }
    }

    public long? Duration
    {
        get => _current.Value?.Context?.Duration;
        set
        {
            EnsureContext().Duration = value;
        }
    }

    public string? ErrorMessage
    {
        get => _current.Value?.Context?.ErrorMessage;
        set
        {
            EnsureContext().ErrorMessage = value;
        }
    }

    public bool IsEnabled
    {
        get => _current.Value?.Context?.IsEnabled ?? true;
        set
        {
            EnsureContext().IsEnabled = value;
        }
    }

    public string? CustomOperationType
    {
        get => _current.Value?.Context?.CustomOperationType;
        set
        {
            EnsureContext().CustomOperationType = value;
        }
    }

    /// <summary>
    /// 待写入的审计日志列表（请求作用域隔离，由 AsyncLocal 保证多请求并发安全）
    /// </summary>
    public IList<AuditLogEntry> PendingAuditLogs =>
        _current.Value?.Context?.PendingAuditLogs ?? (IList<AuditLogEntry>)Array.Empty<AuditLogEntry>();

    public void AddPendingAuditLog(AuditLogEntry auditLog)
    {
        EnsureContext().PendingAuditLogs.Add(auditLog);
    }

    public void ClearPendingAuditLogs()
    {
        if (_current.Value?.Context != null)
        {
            _current.Value.Context.PendingAuditLogs.Clear();
        }
    }

    /// <summary>
    /// 重置上下文（请求结束时调用）
    /// </summary>
    public void Reset()
    {
        _current.Value = null!;
    }

    private AuditLogContextData EnsureContext()
    {
        var holder = _current.Value ??= new AuditLogContextHolder();
        return holder.Context ??= new AuditLogContextData();
    }

    private class AuditLogContextHolder
    {
        public AuditLogContextData? Context { get; set; }
    }

    /// <summary>
    /// 审计日志上下文数据
    /// </summary>
    private class AuditLogContextData
    {
        public long? UserId { get; set; }
        public string? UserName { get; set; }
        public string? RealName { get; set; }
        public long? TenantId { get; set; }
        public long? StoreId { get; set; }
        public string? StoreName { get; set; }
        public string? RequestPath { get; set; }
        public string? RequestMethod { get; set; }
        public string? RequestIp { get; set; }
        public string? UserAgent { get; set; }
        public int? ResponseStatus { get; set; }
        public DateTime? RequestTime { get; set; }
        public long? Duration { get; set; }
        public string? ErrorMessage { get; set; }
        public bool IsEnabled { get; set; } = true;
        public string? CustomOperationType { get; set; }

        /// <summary>
        /// 待写入的审计日志列表（随上下文数据存储在 AsyncLocal 中，请求间隔离）
        /// </summary>
        public List<AuditLogEntry> PendingAuditLogs { get; } = new();
    }
}

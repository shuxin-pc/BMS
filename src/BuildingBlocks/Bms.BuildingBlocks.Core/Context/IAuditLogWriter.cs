namespace Bms.BuildingBlocks.Core.Context;

/// <summary>
/// 审计日志写入器抽象
/// 由各业务服务实现，决定审计日志的实际存储位置（如写入本服务库或共享的 System 库）
/// </summary>
public interface IAuditLogWriter
{
    /// <summary>
    /// 写入一批审计日志（在响应完成后由中间件调用）
    /// </summary>
    /// <param name="entries">待写入的审计日志条目</param>
    /// <param name="responseStatus">响应状态码</param>
    /// <param name="duration">请求耗时（毫秒）</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task WriteAsync(IReadOnlyList<AuditLogEntry> entries, int responseStatus, long duration, CancellationToken cancellationToken = default);
}

using Bms.Store.Domain.Entities;

namespace Bms.Store.Application.Services;

/// <summary>
/// 跨店权益操作审计日志服务接口
/// 记录跨店核销、跨店消费、跨店充值、疗程卡转让等高风险操作（文档 6.1 节）
/// 审计日志在调用方事务内写入，事务回滚则审计日志同步回滚（仅记录成功操作）
/// </summary>
public interface ICrossStoreOperationAuditService
{
    /// <summary>
    /// 写入跨店操作审计日志（在调用方事务内执行，不独立 SaveChanges）
    /// 调用方负责填充业务字段，本方法自动补充 IP/UserAgent 等请求上下文信息
    /// </summary>
    /// <param name="logEntry">审计日志实体（Caller 负责填充业务字段）</param>
    Task LogAsync(CrossStoreOperationLog logEntry);
}

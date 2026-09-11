namespace Bms.BuildingBlocks.Core.Context;

/// <summary>
/// 审计日志选项提供者抽象
/// 由各业务服务实现，决定审计日志开关与排除路径的读取来源
/// </summary>
public interface IAuditLogOptionsProvider
{
    /// <summary>
    /// 是否启用审计日志
    /// </summary>
    Task<bool> IsEnabledAsync();

    /// <summary>
    /// 获取排除的请求路径前缀列表（命中前缀的请求不记录审计日志）
    /// </summary>
    Task<IReadOnlyList<string>> GetExcludedPathsAsync();
}

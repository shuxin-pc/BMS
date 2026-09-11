namespace Bms.BuildingBlocks.Core.Context;

/// <summary>
/// 默认审计日志选项提供者
/// 始终启用审计日志并使用默认排除路径，适用于暂未接入系统配置的业务服务
/// </summary>
public class DefaultAuditLogOptionsProvider : IAuditLogOptionsProvider
{
    /// <summary>
    /// 默认排除路径
    /// </summary>
    private static readonly string[] DefaultExcludedPaths =
    {
        "/swagger",
        "/health",
        "/favicon.ico"
    };

    /// <inheritdoc />
    public Task<bool> IsEnabledAsync()
    {
        return Task.FromResult(true);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<string>> GetExcludedPathsAsync()
    {
        return Task.FromResult<IReadOnlyList<string>>(DefaultExcludedPaths);
    }
}

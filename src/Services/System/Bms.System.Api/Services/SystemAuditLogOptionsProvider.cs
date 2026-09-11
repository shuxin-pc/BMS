using Bms.BuildingBlocks.Core.Context;
using Bms.System.Application.Services;

namespace Bms.System.Api.Services;

/// <summary>
/// System 服务审计日志选项提供者
/// 从系统配置读取审计日志开关与排除路径
/// </summary>
public class SystemAuditLogOptionsProvider : IAuditLogOptionsProvider
{
    /// <summary>
    /// 默认排除路径（配置为空时使用）
    /// </summary>
    private static readonly string[] DefaultExcludedPaths =
    {
        "/swagger",
        "/health",
        "/favicon.ico"
    };

    private readonly ISystemConfigService _systemConfigService;

    public SystemAuditLogOptionsProvider(ISystemConfigService systemConfigService)
    {
        _systemConfigService = systemConfigService;
    }

    /// <inheritdoc />
    public Task<bool> IsEnabledAsync()
    {
        return _systemConfigService.GetBoolAsync("EnableAuditLog", true);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetExcludedPathsAsync()
    {
        // 配置格式：逗号分隔的路径，如 "/health,/swagger,/favicon.ico"
        var excludePathsValue = await _systemConfigService.GetValueAsync("AuditLogExcludePaths", "");

        if (string.IsNullOrWhiteSpace(excludePathsValue))
        {
            return DefaultExcludedPaths;
        }

        return excludePathsValue
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
    }
}

namespace Bms.BuildingBlocks.MultiTenant.Models;

/// <summary>
/// 租户信息
/// </summary>
public class TenantInfo
{
    /// <summary>
    /// 租户ID
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 租户编码
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 租户名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 联系人
    /// </summary>
    public string ContactName { get; set; } = string.Empty;

    /// <summary>
    /// 联系电话
    /// </summary>
    public string ContactPhone { get; set; } = string.Empty;

    /// <summary>
    /// 联系人邮箱
    /// </summary>
    public string ContactEmail { get; set; } = string.Empty;

    /// <summary>
    /// 状态：1-启用，0-禁用
    /// </summary>
    public int Status { get; set; } = 1;

    /// <summary>
    /// 隔离级别
    /// </summary>
    public TenantIsolationLevel IsolationLevel { get; set; }

    /// <summary>
    /// 连接字符串（数据库级隔离使用）
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Schema名称（Schema级隔离使用）
    /// </summary>
    public string? SchemaName { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// 到期时间
    /// </summary>
    public DateTime? ExpireTime { get; set; }

    /// <summary>
    /// 可访问的子系统Code列表（逗号分隔），为空表示可访问所有子系统
    /// 例如: "bms-system,bms-production,bms-quality"
    /// </summary>
    public string? AllowedSubsystems { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 获取可访问的子系统列表
    /// </summary>
    public List<string> GetAllowedSubsystemList()
    {
        if (string.IsNullOrEmpty(AllowedSubsystems))
            return new List<string>();

        return AllowedSubsystems.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .ToList();
    }

    /// <summary>
    /// 检查是否可访问指定子系统
    /// </summary>
    public bool CanAccessSubsystem(string subsystemCode)
    {
        var allowedList = GetAllowedSubsystemList();
        // 为空表示可访问所有子系统
        if (allowedList.Count == 0)
            return true;

        return allowedList.Contains(subsystemCode, StringComparer.OrdinalIgnoreCase);
    }
}

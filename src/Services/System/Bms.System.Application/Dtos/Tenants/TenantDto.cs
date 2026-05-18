using System.Text.Json.Serialization;
using Bms.BuildingBlocks.MultiTenant.Models;

namespace Bms.System.Application.Dtos.Tenants;

/// <summary>
/// 租户DTO（对外接口使用 camelCase）
/// </summary>
public class TenantDto
{
    /// <summary>
    /// 租户ID
    /// </summary>
    [JsonPropertyName("id")]
    public long Id { get; set; }

    /// <summary>
    /// 租户编码（唯一）
    /// </summary>
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 租户名称
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 联系人
    /// </summary>
    [JsonPropertyName("contactName")]
    public string ContactName { get; set; } = string.Empty;

    /// <summary>
    /// 联系电话
    /// </summary>
    [JsonPropertyName("contactPhone")]
    public string ContactPhone { get; set; } = string.Empty;

    /// <summary>
    /// 联系人邮箱
    /// </summary>
    [JsonPropertyName("contactEmail")]
    public string ContactEmail { get; set; } = string.Empty;

    /// <summary>
    /// 状态：1-启用，0-禁用
    /// </summary>
    [JsonPropertyName("status")]
    public int Status { get; set; }

    /// <summary>
    /// 隔离级别：1-行级隔离，2-Schema级隔离，3-数据库级隔离
    /// </summary>
    [JsonPropertyName("isolationLevel")]
    public TenantIsolationLevel IsolationLevel { get; set; }

    /// <summary>
    /// 数据库连接字符串（数据库级隔离使用）
    /// </summary>
    [JsonPropertyName("connectionString")]
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Schema名称（Schema级隔离使用）
    /// </summary>
    [JsonPropertyName("schemaName")]
    public string? SchemaName { get; set; }

    /// <summary>
    /// 是否启用（与status等效，用于兼容部分场景）
    /// </summary>
    [JsonPropertyName("isEnabled")]
    public bool IsEnabled { get; set; }

    /// <summary>
    /// 到期时间，租户过期后自动禁用
    /// </summary>
    [JsonPropertyName("expireTime")]
    public DateTime? ExpireTime { get; set; }

    /// <summary>
    /// 允许访问的子系统（逗号分隔，空表示可访问所有子系统）
    /// 例如: "bms-system,bms-production,bms-quality"
    /// </summary>
    [JsonPropertyName("allowedSubsystems")]
    public string? AllowedSubsystems { get; set; }

    /// <summary>
    /// 允许访问的子系统列表（由AllowedSubsystems解析而来）
    /// </summary>
    [JsonPropertyName("allowedSubsystemList")]
    public List<string>? AllowedSubsystemList { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [JsonPropertyName("remark")]
    public string? Remark { get; set; }
}

/// <summary>
/// 创建租户DTO
/// </summary>
public class TenantCreateDto
{
    /// <summary>
    /// 租户编码（唯一），创建后不可修改
    /// </summary>
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 租户名称
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 联系人
    /// </summary>
    [JsonPropertyName("contactName")]
    public string ContactName { get; set; } = string.Empty;

    /// <summary>
    /// 联系电话
    /// </summary>
    [JsonPropertyName("contactPhone")]
    public string ContactPhone { get; set; } = string.Empty;

    /// <summary>
    /// 联系人邮箱
    /// </summary>
    [JsonPropertyName("contactEmail")]
    public string ContactEmail { get; set; } = string.Empty;

    /// <summary>
    /// 状态：1-启用，0-禁用，默认为1
    /// </summary>
    [JsonPropertyName("status")]
    public int Status { get; set; } = 1;

    /// <summary>
    /// 隔离级别：1-行级隔离（默认），2-Schema级隔离，3-数据库级隔离
    /// </summary>
    [JsonPropertyName("isolationLevel")]
    public TenantIsolationLevel IsolationLevel { get; set; }

    /// <summary>
    /// 数据库连接字符串（数据库级隔离时必填）
    /// </summary>
    [JsonPropertyName("connectionString")]
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Schema名称（Schema级隔离时使用，默认使用租户编码小写）
    /// </summary>
    [JsonPropertyName("schemaName")]
    public string? SchemaName { get; set; }

    /// <summary>
    /// 是否启用，默认为true
    /// </summary>
    [JsonPropertyName("isEnabled")]
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 到期时间，为空表示永不过期
    /// </summary>
    [JsonPropertyName("expireTime")]
    public DateTime? ExpireTime { get; set; }

    /// <summary>
    /// 允许访问的子系统（逗号分隔，空表示可访问所有子系统）
    /// </summary>
    [JsonPropertyName("allowedSubsystems")]
    public string? AllowedSubsystems { get; set; }

    /// <summary>
    /// 备注信息
    /// </summary>
    [JsonPropertyName("remark")]
    public string? Remark { get; set; }
}

/// <summary>
/// 更新租户DTO
/// </summary>
public class TenantUpdateDto
{
    /// <summary>
    /// 租户ID
    /// </summary>
    [JsonPropertyName("id")]
    public long Id { get; set; }

    /// <summary>
    /// 租户名称
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 联系人
    /// </summary>
    [JsonPropertyName("contactName")]
    public string ContactName { get; set; } = string.Empty;

    /// <summary>
    /// 联系电话
    /// </summary>
    [JsonPropertyName("contactPhone")]
    public string ContactPhone { get; set; } = string.Empty;

    /// <summary>
    /// 联系人邮箱
    /// </summary>
    [JsonPropertyName("contactEmail")]
    public string ContactEmail { get; set; } = string.Empty;

    /// <summary>
    /// 状态：1-启用，0-禁用
    /// </summary>
    [JsonPropertyName("status")]
    public int Status { get; set; }

    /// <summary>
    /// 隔离级别：1-行级隔离，2-Schema级隔离，3-数据库级隔离
    /// </summary>
    [JsonPropertyName("isolationLevel")]
    public TenantIsolationLevel IsolationLevel { get; set; }

    /// <summary>
    /// 数据库连接字符串（数据库级隔离时使用）
    /// </summary>
    [JsonPropertyName("connectionString")]
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Schema名称（Schema级隔离时使用）
    /// </summary>
    [JsonPropertyName("schemaName")]
    public string? SchemaName { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    [JsonPropertyName("isEnabled")]
    public bool IsEnabled { get; set; }

    /// <summary>
    /// 到期时间，为空表示永不过期
    /// </summary>
    [JsonPropertyName("expireTime")]
    public DateTime? ExpireTime { get; set; }

    /// <summary>
    /// 允许访问的子系统（逗号分隔，空表示可访问所有子系统）
    /// </summary>
    [JsonPropertyName("allowedSubsystems")]
    public string? AllowedSubsystems { get; set; }

    /// <summary>
    /// 备注信息
    /// </summary>
    [JsonPropertyName("remark")]
    public string? Remark { get; set; }
}
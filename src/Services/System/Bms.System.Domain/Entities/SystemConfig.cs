namespace Bms.System.Domain.Entities;

/// <summary>
/// 系统配置实体
/// </summary>
public class SystemConfig : TenantEntity
{
    /// <summary>
    /// 配置键
    /// </summary>
    public string ConfigKey { get; set; } = string.Empty;

    /// <summary>
    /// 配置值
    /// </summary>
    public string? ConfigValue { get; set; }

    /// <summary>
    /// 配置分组
    /// </summary>
    public string? ConfigGroup { get; set; }

    /// <summary>
    /// 描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 是否公共配置（true=全局配置，false=租户级配置）
    /// </summary>
    public bool IsPublic { get; set; }

    /// <summary>
    /// 排序
    /// </summary>
    public int Sort { get; set; }

    /// <summary>
    /// 是否可编辑（非系统配置可由用户修改）
    /// </summary>
    public bool IsEditable { get; set; } = true;

    /// <summary>
    /// 是否系统配置（通过种子数据生成，系统配置禁止删除）
    /// </summary>
    public bool IsSystem { get; set; }
}
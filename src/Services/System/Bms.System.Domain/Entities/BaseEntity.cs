namespace Bms.System.Domain.Entities;

/// <summary>
/// 基础实体基类
/// </summary>
public abstract class BaseEntity
{
    /// <summary>
    /// 主键ID（雪花ID）
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime? UpdatedTime { get; set; }

    /// <summary>
    /// 是否删除（软删除）
    /// </summary>
    public bool IsDeleted { get; set; }
}
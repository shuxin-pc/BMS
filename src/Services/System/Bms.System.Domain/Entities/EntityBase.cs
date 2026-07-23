namespace Bms.System.Domain.Entities;

/// <summary>
/// 基础实体基类（不含软删除）
/// 用于不需要软删除的关联表、日志表等
/// </summary>
public abstract class EntityBase
{
    /// <summary>
    /// 主键ID（雪花ID）
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedTime { get; set; } = DateTime.Now;

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime? UpdatedTime { get; set; }
}
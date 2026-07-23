namespace Bms.System.Application.Dtos.Messages;

/// <summary>
/// 收件箱查询参数
/// </summary>
public class MessageInboxQueryDto
{
    /// <summary>
    /// 页码
    /// </summary>
    public int PageIndex { get; set; } = 1;

    /// <summary>
    /// 每页数量
    /// </summary>
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// 分类筛选（1=系统, 2=业务, 3=公告）
    /// </summary>
    public int? Category { get; set; }

    /// <summary>
    /// 已读筛选（null=全部, false=未读, true=已读）
    /// </summary>
    public bool? IsRead { get; set; }

    /// <summary>
    /// 开始时间
    /// </summary>
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// 结束时间
    /// </summary>
    public DateTime? EndTime { get; set; }
}

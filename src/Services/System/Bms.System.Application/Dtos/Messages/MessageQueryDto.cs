namespace Bms.System.Application.Dtos.Messages;

/// <summary>
/// 管理端消息发送记录查询参数
/// </summary>
public class MessageQueryDto
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
    /// 分类筛选
    /// </summary>
    public int? Category { get; set; }

    /// <summary>
    /// 来源类型筛选（1=自动, 2=手动）
    /// </summary>
    public int? SourceType { get; set; }

    /// <summary>
    /// 撤回状态筛选（null=全部, true=已撤回, false=正常）
    /// </summary>
    public bool? IsRecalled { get; set; }

    /// <summary>
    /// 租户筛选（仅超级管理员生效，null=所有租户）
    /// </summary>
    public long? TenantId { get; set; }

    /// <summary>
    /// 开始时间
    /// </summary>
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// 结束时间
    /// </summary>
    public DateTime? EndTime { get; set; }
}

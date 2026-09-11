using Bms.Store.Application.Dtos;

namespace Bms.Store.Application.Dtos.Appointments;

/// <summary>
/// 预约分页查询参数
/// </summary>
public class AppointmentQueryDto : PagedRequestDto
{
    /// <summary>
    /// 客户ID
    /// </summary>
    public long? CustomerId { get; set; }

    /// <summary>
    /// 客户名称或手机号关键字（模糊匹配，OR 语义：命中姓名或手机号其一即满足）
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// 预约编号（模糊匹配）
    /// </summary>
    public string? AppointmentNo { get; set; }

    /// <summary>
    /// 预约状态（1:已预约 2:已到店 3:已完成 4:已取消 5:爽约）
    /// </summary>
    public int? Status { get; set; }

    /// <summary>
    /// 预约状态集合（IN 查询，与 Status 单值过滤叠加生效；如快速开单转单需同时匹配 1已预约/2已到店）
    /// 前端以逗号分隔字符串传递，如 statuses=1,2
    /// </summary>
    public List<int>? Statuses { get; set; }

    /// <summary>
    /// 预约开始日期起始（按 StartTime 的日期部分过滤）
    /// </summary>
    public DateTime? StartTimeStart { get; set; }

    /// <summary>
    /// 预约开始日期截止（按 StartTime 的日期部分过滤）
    /// </summary>
    public DateTime? StartTimeEnd { get; set; }

    /// <summary>
    /// 技师来源筛选（1:商家技师 2:平台技师）
    /// </summary>
    public int? TechnicianSource { get; set; }
}

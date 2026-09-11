using Bms.Store.Application.Dtos;

namespace Bms.Store.Application.Dtos.Technicians;

/// <summary>
/// 商家技师分页查询 DTO
/// </summary>
public class TechnicianQueryDto : PagedRequestDto
{
    /// <summary>
    /// 技师姓名或手机号关键字（模糊匹配，OR 语义：命中姓名或手机号其一即满足）
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// 状态筛选（1:在岗 2:休息）
    /// </summary>
    public int? Status { get; set; }

    /// <summary>
    /// 技师来源（1:商家技师 2:平台技师）
    /// </summary>
    public int? Source { get; set; }
}

using Bms.Store.Application.Dtos;

namespace Bms.Store.Application.Dtos.Technicians;

/// <summary>
/// 商家技师分页查询 DTO
/// </summary>
public class TechnicianQueryDto : PagedRequestDto
{
    /// <summary>
    /// 技师姓名（模糊查询）
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// 手机号
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// 状态筛选（1:在岗 2:休息）
    /// </summary>
    public int? Status { get; set; }

    /// <summary>
    /// 技师来源（1:商家技师 2:平台技师）
    /// </summary>
    public int? Source { get; set; }
}

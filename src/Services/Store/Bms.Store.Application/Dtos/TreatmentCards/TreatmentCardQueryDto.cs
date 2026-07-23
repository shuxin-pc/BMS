using Bms.Store.Application.Dtos;

namespace Bms.Store.Application.Dtos.TreatmentCards;

/// <summary>
/// 疗程卡配置分页查询 DTO
/// </summary>
public class TreatmentCardQueryDto : PagedRequestDto
{
    /// <summary>
    /// 卡名称（模糊搜索）
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// 卡编码
    /// </summary>
    public string? Code { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool? IsEnabled { get; set; }
}

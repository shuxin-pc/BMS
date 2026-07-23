using Bms.Store.Application.Dtos;

namespace Bms.Store.Application.Dtos.TreatmentCards;

/// <summary>
/// 疗程卡项目关联分页查询 DTO
/// </summary>
public class CourseCardItemQueryDto : PagedRequestDto
{
    /// <summary>
    /// 疗程卡ID
    /// </summary>
    public long? CourseCardId { get; set; }
}

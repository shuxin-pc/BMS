namespace Bms.Store.Application.Dtos.TreatmentCards;

/// <summary>
/// 更新疗程卡项目关联请求 DTO
/// </summary>
public class CourseCardItemUpdateDto : CourseCardItemCreateDto
{
    /// <summary>
    /// 关联记录ID
    /// </summary>
    public long Id { get; set; }
}

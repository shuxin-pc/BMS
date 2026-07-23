using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.TreatmentCards;

namespace Bms.Store.Application.Services;

/// <summary>
/// 疗程卡项目关联应用服务接口
/// </summary>
public interface ICourseCardItemAppService
{
    Task<ApiResponseDto<PagedResponseDto<CourseCardItemDto>>> GetPagedListAsync(CourseCardItemQueryDto query);
    Task<ApiResponseDto<CourseCardItemDto?>> GetByIdAsync(long id);
    Task<ApiResponseDto<CourseCardItemDto>> CreateAsync(CourseCardItemCreateDto dto);
    Task<ApiResponseDto<CourseCardItemDto>> UpdateAsync(CourseCardItemUpdateDto dto);
    Task<ApiResponseDto> DeleteAsync(long id);
    Task<ApiResponseDto> BatchDeleteAsync(List<long> ids);
}

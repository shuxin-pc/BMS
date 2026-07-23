using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.PointsRules;

namespace Bms.Store.Application.Services;

/// <summary>
/// 积分规则应用服务接口
/// </summary>
public interface IPointsRuleAppService
{
    Task<ApiResponseDto<PagedResponseDto<PointsRuleDto>>> GetPagedListAsync(PointsRuleQueryDto query);
    Task<ApiResponseDto<PointsRuleDto?>> GetByIdAsync(long id);
    Task<ApiResponseDto<PointsRuleDto>> CreateAsync(PointsRuleCreateDto dto);
    Task<ApiResponseDto<PointsRuleDto>> UpdateAsync(PointsRuleUpdateDto dto);
    Task<ApiResponseDto> DeleteAsync(long id);
    Task<ApiResponseDto> BatchDeleteAsync(List<long> ids);
}

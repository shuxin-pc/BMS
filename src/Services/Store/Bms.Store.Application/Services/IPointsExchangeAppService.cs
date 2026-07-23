using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Points;

namespace Bms.Store.Application.Services;

/// <summary>
/// 积分兑换记录应用服务接口
/// </summary>
public interface IPointsExchangeAppService
{
    Task<ApiResponseDto<PagedResponseDto<PointsExchangeDto>>> GetPagedListAsync(PointsExchangeQueryDto query);
    Task<ApiResponseDto<PointsExchangeDto?>> GetByIdAsync(long id);
    Task<ApiResponseDto<PointsExchangeDto>> CreateAsync(PointsExchangeCreateDto dto);
    Task<ApiResponseDto<PointsExchangeDto>> UpdateAsync(PointsExchangeUpdateDto dto);
    Task<ApiResponseDto> DeleteAsync(long id);
    Task<ApiResponseDto> BatchDeleteAsync(List<long> ids);
}

using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Technicians;

namespace Bms.Store.Application.Services;

/// <summary>
/// 技师统计应用服务接口
/// </summary>
public interface ITechnicianStatisticAppService
{
    Task<ApiResponseDto<PagedResponseDto<TechnicianStatisticDto>>> GetPagedListAsync(TechnicianStatisticQueryDto query);
    Task<ApiResponseDto<TechnicianStatisticDto?>> GetByIdAsync(long id);
    Task<ApiResponseDto<TechnicianStatisticDto>> CreateAsync(TechnicianStatisticCreateDto dto);
    Task<ApiResponseDto<TechnicianStatisticDto>> UpdateAsync(TechnicianStatisticUpdateDto dto);
    Task<ApiResponseDto> DeleteAsync(long id);
    Task<ApiResponseDto> BatchDeleteAsync(List<long> ids);

    /// <summary>
    /// 获取技师业绩统计报表（从 OrderItem 实时聚合，仅商家技师）
    /// 对应需求 B4.5：服务人次、时长、回头客率、技师服务费用汇总
    /// 纯平台技师门店（无自有技师）返回 IsPurePlatformStore=true + 空列表，业绩由平台统一统计
    /// </summary>
    Task<ApiResponseDto<TechnicianStatReportDto>> GetReportAsync(TechnicianStatisticQueryDto query);
}

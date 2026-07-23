using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Customers;

namespace Bms.Store.Application.Services;

public interface IBodyDataRecordAppService
{
    Task<ApiResponseDto<PagedResponseDto<BodyDataRecordDto>>> GetPagedListAsync(BodyDataRecordQueryDto query);
    Task<ApiResponseDto<BodyDataRecordDto?>> GetByIdAsync(long id);
    Task<ApiResponseDto<BodyDataRecordDto>> CreateAsync(BodyDataRecordCreateDto dto);
    Task<ApiResponseDto<BodyDataRecordDto>> UpdateAsync(BodyDataRecordUpdateDto dto);
    Task<ApiResponseDto> DeleteAsync(long id);
    Task<ApiResponseDto> BatchDeleteAsync(List<long> ids);

    /// <summary>
    /// 获取客户最近一次身体数据记录
    /// </summary>
    Task<ApiResponseDto<BodyDataRecordDto?>> GetLatestAsync(long customerId);

    /// <summary>
    /// 按时间范围获取身体数据趋势
    /// </summary>
    Task<ApiResponseDto<BodyDataTrendDto>> GetTrendAsync(long customerId, DateTime startDate, DateTime endDate);

    /// <summary>
    /// 对比两个时间点的身体数据差异
    /// </summary>
    Task<ApiResponseDto<BodyDataComparisonDto>> GetComparisonAsync(long customerId, DateTime startDate, DateTime endDate);
}

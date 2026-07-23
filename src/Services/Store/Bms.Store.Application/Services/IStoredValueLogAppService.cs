using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.StoredValues;

namespace Bms.Store.Application.Services;

/// <summary>
/// 储值流水应用服务接口
/// </summary>
public interface IStoredValueLogAppService
{
    Task<ApiResponseDto<PagedResponseDto<StoredValueLogDto>>> GetPagedListAsync(StoredValueLogQueryDto query);
    Task<ApiResponseDto<StoredValueLogDto?>> GetByIdAsync(long id);
    Task<ApiResponseDto<StoredValueLogDto>> CreateAsync(StoredValueLogCreateDto dto);
    Task<ApiResponseDto<StoredValueLogDto>> UpdateAsync(StoredValueLogUpdateDto dto);
    Task<ApiResponseDto> DeleteAsync(long id);
    Task<ApiResponseDto> BatchDeleteAsync(List<long> ids);

    /// <summary>
    /// 储值现金流统计（G7.3）：按日期范围统计新增储值/消费/退款/沉淀资金
    /// </summary>
    Task<ApiResponseDto<StoredValueCashFlowDto>> GetCashFlowAsync(DateTime? startDate, DateTime? endDate);
}

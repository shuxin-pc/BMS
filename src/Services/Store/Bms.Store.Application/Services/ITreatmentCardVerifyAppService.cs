using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.TreatmentCards;

namespace Bms.Store.Application.Services;

/// <summary>
/// 疗程卡核销记录应用服务接口
/// 注意：核销记录不允许直接删除/批量删除，反核销需通过订单取消/退款接口完成事务回滚
/// （直接删除会遗留孤儿订单且不回滚库存/BOM/疗程卡次数）
/// </summary>
public interface ITreatmentCardVerifyAppService
{
    Task<ApiResponseDto<PagedResponseDto<TreatmentCardVerifyDto>>> GetPagedListAsync(TreatmentCardVerifyQueryDto query);
    Task<ApiResponseDto<TreatmentCardVerifyDto?>> GetByIdAsync(long id);
    Task<ApiResponseDto<TreatmentCardVerifyDto>> CreateAsync(TreatmentCardVerifyCreateDto dto);
    Task<ApiResponseDto<TreatmentCardVerifyDto>> UpdateAsync(TreatmentCardVerifyUpdateDto dto);
}

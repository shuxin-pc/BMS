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

    /// <summary>
    /// 核销冲正（规则7）
    /// 通过 ReverseStatus 状态机实现，不物理删除核销记录
    /// 冲正时：恢复疗程卡剩余次数、冲减累计消费金额、取消关联订单
    /// 冲正金额冲减原核销门店服务业绩
    /// </summary>
    Task<ApiResponseDto> ReverseAsync(TreatmentCardVerifyReverseDto dto);
}

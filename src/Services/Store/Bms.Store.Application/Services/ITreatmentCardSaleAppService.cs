using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.TreatmentCards;

namespace Bms.Store.Application.Services;

/// <summary>
/// 疗程卡销售记录应用服务接口
/// </summary>
public interface ITreatmentCardSaleAppService
{
    Task<ApiResponseDto<PagedResponseDto<TreatmentCardSaleDto>>> GetPagedListAsync(TreatmentCardSaleQueryDto query);
    Task<ApiResponseDto<TreatmentCardSaleDto?>> GetByIdAsync(long id);
    Task<ApiResponseDto<TreatmentCardSaleDto>> CreateAsync(TreatmentCardSaleCreateDto dto);
    Task<ApiResponseDto<TreatmentCardSaleDto>> UpdateAsync(TreatmentCardSaleUpdateDto dto);
    Task<ApiResponseDto> DeleteAsync(long id);
    Task<ApiResponseDto> BatchDeleteAsync(List<long> ids);

    /// <summary>
    /// 获取疗程卡到期提醒分页列表
    /// </summary>
    Task<ApiResponseDto<PagedResponseDto<TreatmentCardExpiryDto>>> GetExpiryListAsync(TreatmentCardExpiryQueryDto query);

    /// <summary>
    /// 退卡（规则6）
    /// 全额冲减发卡门店销售业绩，已发生的核销业绩不冲回
    /// 退卡金额 = 售价 - 已核销金额
    /// </summary>
    Task<ApiResponseDto<TreatmentCardSaleRefundResultDto>> RefundAsync(TreatmentCardSaleRefundDto dto);
}

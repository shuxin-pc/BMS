using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.StoredValues;

namespace Bms.Store.Application.Services;

/// <summary>
/// 储值规则应用服务接口
/// </summary>
public interface IStoredValueRuleAppService
{
    Task<ApiResponseDto<PagedResponseDto<StoredValueRuleDto>>> GetPagedListAsync(StoredValueRuleQueryDto query);
    Task<ApiResponseDto<StoredValueRuleDto?>> GetByIdAsync(long id);
    Task<ApiResponseDto<StoredValueRuleDto>> CreateAsync(StoredValueRuleCreateDto dto);
    Task<ApiResponseDto<StoredValueRuleDto>> UpdateAsync(StoredValueRuleUpdateDto dto);
    Task<ApiResponseDto> DeleteAsync(long id);
    Task<ApiResponseDto> BatchDeleteAsync(List<long> ids);

    /// <summary>
    /// 试算充值赠送金额（与实际充值使用同一计算口径）
    /// </summary>
    Task<ApiResponseDto<StoredValueGiftPreviewDto>> PreviewGiftAmountAsync(decimal amount);
}

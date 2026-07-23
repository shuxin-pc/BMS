using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.TreatmentCardTransfers;

namespace Bms.Store.Application.Services;

/// <summary>
/// 疗程卡转让管理应用服务接口
/// </summary>
public interface ITreatmentCardTransferAppService
{
    /// <summary>
    /// 获取疗程卡转让记录分页列表
    /// </summary>
    Task<ApiResponseDto<PagedResponseDto<TreatmentCardTransferDto>>> GetPagedListAsync(TreatmentCardTransferQueryDto query);

    /// <summary>
    /// 根据ID获取疗程卡转让记录详情
    /// </summary>
    Task<ApiResponseDto<TreatmentCardTransferDto?>> GetByIdAsync(long id);

    /// <summary>
    /// 创建疗程卡转让记录
    /// </summary>
    Task<ApiResponseDto<TreatmentCardTransferDto>> CreateAsync(TreatmentCardTransferCreateDto dto);

    /// <summary>
    /// 更新疗程卡转让记录
    /// </summary>
    Task<ApiResponseDto<TreatmentCardTransferDto>> UpdateAsync(TreatmentCardTransferUpdateDto dto);

    /// <summary>
    /// 删除疗程卡转让记录（软删除）
    /// </summary>
    Task<ApiResponseDto> DeleteAsync(long id);

    /// <summary>
    /// 批量删除疗程卡转让记录（软删除）
    /// </summary>
    Task<ApiResponseDto> BatchDeleteAsync(List<long> ids);
}

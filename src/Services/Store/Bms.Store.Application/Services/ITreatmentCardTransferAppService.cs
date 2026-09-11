using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.TreatmentCardTransfers;

namespace Bms.Store.Application.Services;

/// <summary>
/// 项目卡转让管理应用服务接口
/// </summary>
public interface ITreatmentCardTransferAppService
{
    /// <summary>
    /// 获取项目卡转让记录分页列表
    /// </summary>
    Task<ApiResponseDto<PagedResponseDto<TreatmentCardTransferDto>>> GetPagedListAsync(TreatmentCardTransferQueryDto query);

    /// <summary>
    /// 获取可转让的项目卡销售记录选项（转卡弹窗选择用）
    /// 仅返回状态有效(1)且剩余次数大于 0 的卡销售记录，跨店购卡均可见
    /// </summary>
    Task<ApiResponseDto<List<TreatmentCardTransferOptionDto>>> GetTransferableCardSalesAsync();

    /// <summary>
    /// 根据ID获取项目卡转让记录详情
    /// </summary>
    Task<ApiResponseDto<TreatmentCardTransferDto?>> GetByIdAsync(long id);

    /// <summary>
    /// 创建项目卡转让记录
    /// </summary>
    Task<ApiResponseDto<TreatmentCardTransferDto>> CreateAsync(TreatmentCardTransferCreateDto dto);

    /// <summary>
    /// 更新项目卡转让记录
    /// </summary>
    Task<ApiResponseDto<TreatmentCardTransferDto>> UpdateAsync(TreatmentCardTransferUpdateDto dto);

    /// <summary>
    /// 删除项目卡转让记录（软删除）
    /// </summary>
    Task<ApiResponseDto> DeleteAsync(long id);

    /// <summary>
    /// 批量删除项目卡转让记录（软删除）
    /// </summary>
    Task<ApiResponseDto> BatchDeleteAsync(List<long> ids);
}

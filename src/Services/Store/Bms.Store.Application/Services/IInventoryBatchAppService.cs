using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.InventoryBatches;

namespace Bms.Store.Application.Services;

/// <summary>
/// 库存批次应用服务接口
/// </summary>
public interface IInventoryBatchAppService
{
    Task<ApiResponseDto<PagedResponseDto<InventoryBatchDto>>> GetPagedListAsync(InventoryBatchQueryDto query);
    Task<ApiResponseDto<InventoryBatchDto?>> GetByIdAsync(long id);
    Task<ApiResponseDto<InventoryBatchDto>> CreateAsync(InventoryBatchCreateDto dto);
    Task<ApiResponseDto<InventoryBatchDto>> UpdateAsync(InventoryBatchUpdateDto dto);
    Task<ApiResponseDto> DeleteAsync(long id);
    Task<ApiResponseDto> BatchDeleteAsync(List<long> ids);

    /// <summary>
    /// 获取效期信息分页列表
    /// </summary>
    Task<ApiResponseDto<PagedResponseDto<ExpiryDto>>> GetExpiryListAsync(ExpiryQueryDto query);

    /// <summary>
    /// 获取效期预警列表（即将过期或已过期的商品）
    /// </summary>
    Task<ApiResponseDto<PagedResponseDto<ExpiryDto>>> GetExpiryAlertsAsync(ExpiryQueryDto query);

    /// <summary>
    /// 按商品ID查询可用效期选项列表（用于 POS 效期选择）。
    /// 有日期批次按过期日期升序在前，无效期批次排末尾按 CreatedTime 升序，第一项标记为推荐。
    /// </summary>
    Task<ApiResponseDto<List<ProductExpiryOptionDto>>> GetExpiryOptionsByProductIdAsync(long productId);
}

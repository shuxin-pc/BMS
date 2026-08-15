using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Inventories;

namespace Bms.Store.Application.Services;

/// <summary>
/// 库存预警应用服务接口
/// </summary>
public interface IInventoryAlertAppService
{
    Task<ApiResponseDto<PagedResponseDto<InventoryAlertDto>>> GetPagedListAsync(InventoryAlertQueryDto query);
    Task<ApiResponseDto<InventoryAlertDto?>> GetByIdAsync(long id);
    Task<ApiResponseDto<InventoryAlertDto>> CreateAsync(InventoryAlertCreateDto dto);
    Task<ApiResponseDto<InventoryAlertDto>> UpdateAsync(InventoryAlertUpdateDto dto);
    Task<ApiResponseDto> DeleteAsync(long id);
    Task<ApiResponseDto> BatchDeleteAsync(List<long> ids);

    /// <summary>
    /// 全量扫描并生成预警（低库存/效期/积压），同时关闭已解决的预警（定时兜底）
    /// </summary>
    Task<ApiResponseDto<InventoryAlertScanResultDto>> ScanAsync();

    /// <summary>
    /// 即时检测指定商品的预警（库存变动后调用）
    /// 双向处理低库存/积压预警的生成与关闭，并关闭已用完批次的效期预警
    /// </summary>
    Task CheckInventoryAlertsAsync(long tenantId, long storeId, long productId);
}

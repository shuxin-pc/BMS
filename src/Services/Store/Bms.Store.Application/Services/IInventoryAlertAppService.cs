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
    /// 全量扫描并生成预警（低库存/效期/积压），返回新生成预警数量
    /// </summary>
    Task<ApiResponseDto<InventoryAlertScanResultDto>> ScanAsync();

    /// <summary>
    /// 即时检测指定商品的低库存预警（库存变动后调用）
    /// </summary>
    Task CheckLowStockAsync(long tenantId, long storeId, long productId);

    /// <summary>
    /// 标记预警已处理
    /// </summary>
    Task<ApiResponseDto> ProcessAsync(long id, string? remark);
}

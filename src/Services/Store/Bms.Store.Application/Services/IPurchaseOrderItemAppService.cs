using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.PurchaseOrders;

namespace Bms.Store.Application.Services;

/// <summary>
/// 采购订单明细应用服务接口
/// </summary>
public interface IPurchaseOrderItemAppService
{
    Task<ApiResponseDto<PagedResponseDto<PurchaseOrderItemDto>>> GetPagedListAsync(PurchaseOrderItemQueryDto query);
    Task<ApiResponseDto<PurchaseOrderItemDto?>> GetByIdAsync(long id);
    Task<ApiResponseDto<PurchaseOrderItemDto>> CreateAsync(PurchaseOrderItemCreateDto dto);
    Task<ApiResponseDto<PurchaseOrderItemDto>> UpdateAsync(PurchaseOrderItemUpdateDto dto);
    Task<ApiResponseDto> DeleteAsync(long id);
    Task<ApiResponseDto> BatchDeleteAsync(List<long> ids);
}

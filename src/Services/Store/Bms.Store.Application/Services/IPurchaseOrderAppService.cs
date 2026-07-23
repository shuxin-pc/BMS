using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.PurchaseOrders;

namespace Bms.Store.Application.Services;

/// <summary>
/// 采购订单应用服务接口
/// </summary>
public interface IPurchaseOrderAppService
{
    Task<ApiResponseDto<PagedResponseDto<PurchaseOrderDto>>> GetPagedListAsync(PurchaseOrderQueryDto query);
    Task<ApiResponseDto<PurchaseOrderDto?>> GetByIdAsync(long id);
    Task<ApiResponseDto<PurchaseOrderDto>> CreateAsync(PurchaseOrderCreateDto dto);
    Task<ApiResponseDto<PurchaseOrderDto>> UpdateAsync(PurchaseOrderUpdateDto dto);
    Task<ApiResponseDto> DeleteAsync(long id);
    Task<ApiResponseDto> BatchDeleteAsync(List<long> ids);
}

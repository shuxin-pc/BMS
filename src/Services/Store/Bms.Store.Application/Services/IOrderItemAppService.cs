using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Orders;

namespace Bms.Store.Application.Services;

/// <summary>
/// 订单明细应用服务接口
/// </summary>
public interface IOrderItemAppService
{
    Task<ApiResponseDto<PagedResponseDto<OrderItemDto>>> GetPagedListAsync(OrderItemQueryDto query);
    Task<ApiResponseDto<OrderItemDto?>> GetByIdAsync(long id);
    Task<ApiResponseDto<OrderItemDto>> CreateAsync(OrderItemCreateDto dto);
    Task<ApiResponseDto<OrderItemDto>> UpdateAsync(OrderItemUpdateDto dto);
    Task<ApiResponseDto> DeleteAsync(long id);
    Task<ApiResponseDto> BatchDeleteAsync(List<long> ids);
}

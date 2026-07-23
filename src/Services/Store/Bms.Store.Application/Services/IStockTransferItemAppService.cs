using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.StockTransfers;

namespace Bms.Store.Application.Services;

/// <summary>
/// 库存调拨单明细应用服务接口
/// </summary>
public interface IStockTransferItemAppService
{
    Task<ApiResponseDto<PagedResponseDto<StockTransferItemDto>>> GetPagedListAsync(StockTransferItemQueryDto query);
    Task<ApiResponseDto<StockTransferItemDto?>> GetByIdAsync(long id);
    Task<ApiResponseDto<StockTransferItemDto>> CreateAsync(StockTransferItemCreateDto dto);
    Task<ApiResponseDto<StockTransferItemDto>> UpdateAsync(StockTransferItemUpdateDto dto);
    Task<ApiResponseDto> DeleteAsync(long id);
    Task<ApiResponseDto> BatchDeleteAsync(List<long> ids);
}

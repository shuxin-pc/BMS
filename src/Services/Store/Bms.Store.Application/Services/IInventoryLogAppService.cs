using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Inventories;

namespace Bms.Store.Application.Services;

/// <summary>
/// 库存流水应用服务接口
/// </summary>
public interface IInventoryLogAppService
{
    Task<ApiResponseDto<PagedResponseDto<InventoryLogDto>>> GetPagedListAsync(InventoryLogQueryDto query);
    Task<ApiResponseDto<InventoryLogDto?>> GetByIdAsync(long id);
    Task<ApiResponseDto<InventoryLogDto>> CreateAsync(InventoryLogCreateDto dto);
    Task<ApiResponseDto<InventoryLogDto>> UpdateAsync(InventoryLogUpdateDto dto);
    Task<ApiResponseDto> DeleteAsync(long id);
    Task<ApiResponseDto> BatchDeleteAsync(List<long> ids);
}

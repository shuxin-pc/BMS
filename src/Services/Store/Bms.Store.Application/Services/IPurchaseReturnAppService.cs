using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.PurchaseReturns;

namespace Bms.Store.Application.Services;

/// <summary>
/// 采购退货应用服务接口
/// </summary>
public interface IPurchaseReturnAppService
{
    Task<ApiResponseDto<PagedResponseDto<PurchaseReturnDto>>> GetPagedListAsync(PurchaseReturnQueryDto query);
    Task<ApiResponseDto<PurchaseReturnDto?>> GetByIdAsync(long id);
    Task<ApiResponseDto<PurchaseReturnDto>> CreateAsync(PurchaseReturnCreateDto dto);
    Task<ApiResponseDto<PurchaseReturnDto>> UpdateAsync(PurchaseReturnUpdateDto dto);
    Task<ApiResponseDto> DeleteAsync(long id);
    Task<ApiResponseDto> BatchDeleteAsync(List<long> ids);
}

using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.PriceChangeLogs;

namespace Bms.Store.Application.Services;

/// <summary>
/// 价格变更记录应用服务接口
/// </summary>
public interface IPriceChangeLogAppService
{
    Task<ApiResponseDto<PagedResponseDto<PriceChangeLogDto>>> GetPagedListAsync(PriceChangeLogQueryDto query);
    Task<ApiResponseDto<PriceChangeLogDto?>> GetByIdAsync(long id);
}

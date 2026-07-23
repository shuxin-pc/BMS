using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.StockTransfers;

namespace Bms.Store.Application.Services;

/// <summary>
/// 库存调拨单应用服务接口
/// 状态机闭环：待调出(草稿) -> 已调入(已完成) / 已取消
/// </summary>
public interface IStockTransferAppService
{
    Task<ApiResponseDto<PagedResponseDto<StockTransferDto>>> GetPagedListAsync(StockTransferQueryDto query);
    Task<ApiResponseDto<StockTransferDto?>> GetByIdAsync(long id);

    /// <summary>
    /// 创建调拨单（草稿状态：待调出，不调整库存）
    /// </summary>
    Task<ApiResponseDto<StockTransferDto>> CreateAsync(StockTransferCreateDto dto);

    Task<ApiResponseDto<StockTransferDto>> UpdateAsync(StockTransferUpdateDto dto);
    Task<ApiResponseDto> DeleteAsync(long id);
    Task<ApiResponseDto> BatchDeleteAsync(List<long> ids);

    /// <summary>
    /// 执行调拨：同一事务内调出门店扣减 + 调入门店增加，写两条 InventoryLog，状态转已调入（已完成）
    /// 仅待调出（草稿）状态可执行
    /// </summary>
    Task<ApiResponseDto> ExecuteAsync(long id);

    /// <summary>
    /// 取消调拨单：草稿转已取消（已调入的不可取消，需走反向调拨单）
    /// </summary>
    Task<ApiResponseDto> CancelAsync(long id, string? reason);
}

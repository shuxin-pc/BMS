using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Orders;

namespace Bms.Store.Application.Services;

/// <summary>
/// 订单应用服务接口
/// </summary>
public interface IOrderAppService
{
    Task<ApiResponseDto<PagedResponseDto<OrderDto>>> GetPagedListAsync(OrderQueryDto query);
    Task<ApiResponseDto<OrderDto?>> GetByIdAsync(long id);
    Task<ApiResponseDto<OrderDto>> CreateAsync(OrderCreateDto dto);

    /// <summary>
    /// 取消订单（事务包裹，按 OrderType 全量回滚库存/BOM/疗程卡/积分/储值/统计/消费记录）
    /// 订单 Status 改为 4（已取消），视为订单未发生
    /// </summary>
    Task<ApiResponseDto> CancelAsync(long id, OrderCancelDto dto);

    Task<ApiResponseDto<RefundResultDto>> RefundAsync(RefundRequestDto dto);

    /// <summary>
    /// 获取今日待补录订单列表（为站内信功能预留数据源）
    /// 查询条件：当前租户下 BackfillStatus=1 且 OrderTime <= 今日的订单
    /// </summary>
    Task<ApiResponseDto<List<OrderDto>>> GetTodayBackfillOrdersAsync();
}

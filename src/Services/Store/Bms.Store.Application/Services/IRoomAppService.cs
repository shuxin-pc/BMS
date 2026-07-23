using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Rooms;

namespace Bms.Store.Application.Services;

/// <summary>
/// 房间床位应用服务接口
/// </summary>
public interface IRoomAppService
{
    Task<ApiResponseDto<PagedResponseDto<RoomDto>>> GetPagedListAsync(RoomQueryDto query);
    Task<ApiResponseDto<RoomDto?>> GetByIdAsync(long id);
    Task<ApiResponseDto<RoomDto>> CreateAsync(RoomCreateDto dto);
    Task<ApiResponseDto<RoomDto>> UpdateAsync(RoomUpdateDto dto);
    Task<ApiResponseDto> DeleteAsync(long id);
    Task<ApiResponseDto> BatchDeleteAsync(List<long> ids);

    /// <summary>
    /// 根据服务项目查询可用房间/床位列表
    /// 按服务项目 ServiceProduct.RequiredRoomType 过滤房间类型，并排除指定时段已冲突的房间
    /// </summary>
    /// <param name="serviceProductId">服务项目商品ID（关联 Product 主表）</param>
    /// <param name="startTime">预约开始时间</param>
    /// <param name="endTime">预约结束时间</param>
    /// <param name="excludeAppointmentId">需排除的预约ID（更新场景，避免与自身冲突）</param>
    /// <returns>可用房间列表</returns>
    Task<ApiResponseDto<List<RoomDto>>> GetAvailableByServiceProductAsync(
        long serviceProductId,
        DateTime startTime,
        DateTime endTime,
        long? excludeAppointmentId = null);
}


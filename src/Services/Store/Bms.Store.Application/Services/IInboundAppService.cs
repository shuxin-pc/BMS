using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Inventories;

namespace Bms.Store.Application.Services;

/// <summary>
/// 入库应用服务接口
/// 负责语义化入库端点的业务逻辑：事务内写 InventoryLog + InventoryBatch + Inventory 三表
/// </summary>
public interface IInboundAppService
{
    /// <summary>
    /// 创建入库记录（事务内同步维护批次表与库存汇总表）
    /// </summary>
    /// <param name="dto">入库请求 DTO</param>
    /// <returns>带显示字段的库存流水 DTO</returns>
    Task<ApiResponseDto<InventoryLogDto>> CreateAsync(InboundCreateDto dto);
}

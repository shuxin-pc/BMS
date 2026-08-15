using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.PurchaseOrders;

namespace Bms.Store.Application.Services;

/// <summary>
/// 采购订单应用服务接口
/// 采购订单创建即入库，单据不可变，仅支持查询与创建
/// </summary>
public interface IPurchaseOrderAppService
{
    /// <summary>
    /// 获取采购订单分页列表
    /// </summary>
    Task<ApiResponseDto<PagedResponseDto<PurchaseOrderDto>>> GetPagedListAsync(PurchaseOrderQueryDto query);

    /// <summary>
    /// 根据ID获取采购订单详情（含明细）
    /// </summary>
    Task<ApiResponseDto<PurchaseOrderDto?>> GetByIdAsync(long id);

    /// <summary>
    /// 创建采购订单（创建即入库，联动库存：创建批次、更新汇总、记录流水）
    /// </summary>
    Task<ApiResponseDto<PurchaseOrderDto>> CreateAsync(PurchaseOrderCreateDto dto);
}

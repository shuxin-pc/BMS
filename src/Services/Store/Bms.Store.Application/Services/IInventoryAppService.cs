using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Inventories;

namespace Bms.Store.Application.Services;

/// <summary>
/// 库存应用服务接口
/// </summary>
public interface IInventoryAppService
{
    Task<ApiResponseDto<PagedResponseDto<InventoryDto>>> GetPagedListAsync(InventoryQueryDto query);
    Task<ApiResponseDto<InventoryDto?>> GetByIdAsync(long id);
    Task<ApiResponseDto<InventoryDto>> CreateAsync(InventoryCreateDto dto);
    Task<ApiResponseDto<InventoryDto>> UpdateAsync(InventoryUpdateDto dto);
    Task<ApiResponseDto> DeleteAsync(long id);
    Task<ApiResponseDto> BatchDeleteAsync(List<long> ids);

    /// <summary>
    /// 按批次扣减库存（用于采购退货等业务出库场景）
    /// 业务规则：
    /// 1. 指定 BatchNo 时扣减对应批次；未指定时按 FIFO（先进先出）扣减该品项的所有在库批次
    /// 2. 写入 InventoryLog（Type=2 出库，SourceType 由调用方传入）
    /// 3. 同步更新 Inventory 汇总表
    /// 4. 库存不足时返回 Fail，调用方应回滚事务
    /// 注意：本方法不开启独立事务，调用方需在外层包裹事务以保证原子性
    /// </summary>
    /// <param name="productId">商品ID</param>
    /// <param name="quantity">扣减数量（正数）</param>
    /// <param name="batchNo">批次号（null=按 FIFO 扣减）</param>
    /// <param name="sourceType">来源类型（见 InventoryLogSourceTypes）</param>
    /// <param name="refId">关联单据ID（如采购退货单ID）</param>
    /// <param name="remark">流水备注</param>
    Task<ApiResponseDto> DeductByBatchAsync(
        long productId,
        decimal quantity,
        string? batchNo,
        int sourceType,
        long? refId,
        string? remark = null);
}

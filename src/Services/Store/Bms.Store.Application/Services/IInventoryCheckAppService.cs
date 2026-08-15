using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Inventories;

namespace Bms.Store.Application.Services;

/// <summary>
/// 库存盘点记录应用服务接口
/// 状态机闭环：创建草稿 -> 提交（已完成）/ 取消（已取消）
/// </summary>
public interface IInventoryCheckAppService
{
    Task<ApiResponseDto<PagedResponseDto<InventoryCheckDto>>> GetPagedListAsync(InventoryCheckQueryDto query);
    Task<ApiResponseDto<InventoryCheckDto?>> GetByIdAsync(long id);

    /// <summary>
    /// 创建盘点单（草稿状态，不调整库存）
    /// </summary>
    Task<ApiResponseDto<InventoryCheckDto>> CreateAsync(InventoryCheckCreateDto dto);

    /// <summary>
    /// 创建并提交盘点单（原子操作）：事务内完成创建+提交，不产生草稿残留
    /// 按差异方向分支：盘亏扣批次/盘盈累加批次/无差异仅记录
    /// </summary>
    Task<ApiResponseDto<InventoryCheckDto>> CreateAndSubmitAsync(CreateAndSubmitCheckDto dto);

    Task<ApiResponseDto<InventoryCheckDto>> UpdateAsync(InventoryCheckUpdateDto dto);
    Task<ApiResponseDto> DeleteAsync(long id);
    Task<ApiResponseDto> BatchDeleteAsync(List<long> ids);

    /// <summary>
    /// 提交盘点单：录入实际数量，计算差异，自动调整库存并写入 InventoryLog，状态转已完成
    /// 仅草稿状态可提交
    /// </summary>
    Task<ApiResponseDto<InventoryCheckDto>> SubmitCheckAsync(SubmitCheckDto dto);

    /// <summary>
    /// 取消盘点单：草稿转已取消（已完成的不可取消）
    /// </summary>
    Task<ApiResponseDto> CancelAsync(long id);

    /// <summary>
    /// 获取盘点专用商品选项（含当前门店账面库存和成本价，用于新增盘点下拉选择）
    /// </summary>
    Task<ApiResponseDto<List<InventoryCheckProductOptionDto>>> GetProductOptionsForCheckAsync();

    /// <summary>
    /// 盘盈批次号查询：按 productId + batchNo 查当前商品在当前门店的全部历史批次（含已扣完）
    /// 用于盘盈录入时校验批次号存在性并带出批次属性
    /// </summary>
    /// <returns>找到返回批次详情，找不到返回 null</returns>
    Task<ApiResponseDto<InventoryCheckBatchLookupDto?>> GetBatchLookupForCheckAsync(long productId, string batchNo);

    /// <summary>
    /// 查询当日该商品是否已有非取消状态的盘点记录（用于前端软约束提示）
    /// </summary>
    Task<ApiResponseDto<bool>> HasProductCheckedTodayAsync(long productId);
}

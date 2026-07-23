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
}

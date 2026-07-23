using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Stores;

namespace Bms.Store.Application.Services;

/// <summary>
/// 门店管理应用服务接口
/// </summary>
public interface IStoreAppService
{
    /// <summary>
    /// 获取门店分页列表
    /// </summary>
    Task<ApiResponseDto<PagedResponseDto<StoreDto>>> GetPagedListAsync(StoreQueryDto query);

    /// <summary>
    /// 根据ID获取门店详情
    /// </summary>
    Task<ApiResponseDto<StoreDto?>> GetByIdAsync(long id);

    /// <summary>
    /// 获取当前租户下授权的门店列表（仅返回启用状态的门店）
    /// </summary>
    Task<ApiResponseDto<List<StoreDto>>> GetAuthorizedStoresAsync();

    /// <summary>
    /// 创建门店
    /// </summary>
    Task<ApiResponseDto<StoreDto>> CreateAsync(StoreCreateDto dto);

    /// <summary>
    /// 更新门店
    /// </summary>
    Task<ApiResponseDto<StoreDto>> UpdateAsync(StoreUpdateDto dto);

    /// <summary>
    /// 删除门店（软删除）
    /// </summary>
    Task<ApiResponseDto> DeleteAsync(long id);

    /// <summary>
    /// 批量删除门店（软删除）
    /// </summary>
    Task<ApiResponseDto> BatchDeleteAsync(List<long> ids);
}

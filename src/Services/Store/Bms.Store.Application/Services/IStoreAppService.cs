using Bms.Store.Application.Abstractions;
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
    /// 获取当前用户授权的门店列表（仅返回启用状态的门店）
    /// super_admin/tenant_admin：返回本租户所有营业中门店
    /// 普通用户：仅返回被分配（UserStores 表）的营业中门店
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

    /// <summary>
    /// 获取门店已分配的用户列表
    /// </summary>
    /// <param name="storeId">门店ID</param>
    Task<ApiResponseDto<List<TenantUserDto>>> GetAssignedUsersAsync(long storeId);

    /// <summary>
    /// 获取门店可分配用户列表（本租户有效用户 + 标记是否已分配）
    /// </summary>
    /// <param name="storeId">门店ID</param>
    Task<ApiResponseDto<List<AvailableUserDto>>> GetAvailableUsersAsync(long storeId);

    /// <summary>
    /// 全量替换门店的用户分配（diff 计算：新增/删除）
    /// 仅 super_admin/tenant_admin 可调用
    /// </summary>
    /// <param name="storeId">门店ID</param>
    /// <param name="userIds">最终选中的用户ID列表</param>
    Task<ApiResponseDto> AssignUsersAsync(long storeId, List<long> userIds);
}

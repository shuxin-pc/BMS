using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.ParkedOrders;

namespace Bms.Store.Application.Services;

/// <summary>
/// POS 挂单应用服务接口
/// 挂单在门店内共享（同门店收银员均可查看/取单），按 TenantId + StoreId 隔离
/// </summary>
public interface IParkedOrderAppService
{
    /// <summary>
    /// 挂单：保存当前购物车快照，生成挂单号，状态置为挂起
    /// </summary>
    Task<ApiResponseDto<ParkedOrderDto>> CreateAsync(ParkedOrderCreateDto dto);

    /// <summary>
    /// 挂单分页列表（仅挂起状态，按挂单时间倒序）
    /// </summary>
    Task<ApiResponseDto<PagedResponseDto<ParkedOrderDto>>> GetPagedListAsync(ParkedOrderQueryDto query);

    /// <summary>
    /// 取单：校验挂单归属门店且为挂起状态，置为已取走并返回完整购物车快照
    /// </summary>
    Task<ApiResponseDto<ParkedOrderDto>> ResumeAsync(long id);

    /// <summary>
    /// 取消挂单：校验挂单归属门店且为挂起状态，置为已取消
    /// </summary>
    Task<ApiResponseDto> CancelAsync(long id);
}

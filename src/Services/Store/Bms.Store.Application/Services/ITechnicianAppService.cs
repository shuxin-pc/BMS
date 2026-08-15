using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Technicians;

namespace Bms.Store.Application.Services;

/// <summary>
/// 商家技师应用服务接口
/// </summary>
public interface ITechnicianAppService
{
    Task<ApiResponseDto<PagedResponseDto<TechnicianDto>>> GetPagedListAsync(TechnicianQueryDto query);
    Task<ApiResponseDto<TechnicianDto?>> GetByIdAsync(long id);
    Task<ApiResponseDto<TechnicianDto>> CreateAsync(TechnicianCreateDto dto);
    Task<ApiResponseDto<TechnicianDto>> UpdateAsync(TechnicianUpdateDto dto);
    Task<ApiResponseDto> DeleteAsync(long id);
    Task<ApiResponseDto> BatchDeleteAsync(List<long> ids);

    /// <summary>
    /// 按服务项目查询可用技师（预约时过滤技师下拉 + 服务项目页展示可服务技师）
    /// 技能匹配：服务项目适用技能与技师技能标签沿技能分类树展开求交集
    /// - serviceProductId/masterId 均为空：返回指定来源全部启用技师
    /// - 服务项目未配置技能：返回全部启用技师
    /// - 平台技师技能无法与当前门店技能分类对齐，保持全部可选
    /// </summary>
    /// <param name="serviceProductId">服务项目子表ID（预约页语境）</param>
    /// <param name="masterId">商品主档ID（服务项目页语境，自动反查租户内 ServiceProduct）</param>
    /// <param name="source">技师来源（1:商家 2:平台，可选）</param>
    Task<ApiResponseDto<List<TechnicianDto>>> GetAvailableByServiceAsync(long? serviceProductId, long? masterId, int? source);

    /// <summary>
    /// 查询技师可服务的服务项目列表（技师页展示擅长项目）
    /// 技师无技能标签：仅返回未限定技能的服务项目
    /// </summary>
    Task<ApiResponseDto<List<TechnicianServiceItemDto>>> GetServiceProductsByTechnicianAsync(long technicianId);
}

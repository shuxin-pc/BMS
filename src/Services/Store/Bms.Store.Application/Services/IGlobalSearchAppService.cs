using Bms.BuildingBlocks.Core.Search;
using Bms.Store.Application.Dtos;

namespace Bms.Store.Application.Services;

/// <summary>
/// 全局搜索应用服务接口（门店业务数据）
/// </summary>
public interface IGlobalSearchAppService
{
    /// <summary>
    /// 全局搜索：跨顾客/订单/商品/项目卡/储值/预约六类数据查询，仅返回有命中的分组
    /// </summary>
    /// <param name="request">搜索请求（关键字 + 每组条数上限）</param>
    Task<ApiResponseDto<List<SearchResultGroupDto>>> SearchAsync(GlobalSearchRequestDto request);
}

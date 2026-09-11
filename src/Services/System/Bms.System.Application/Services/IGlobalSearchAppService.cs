using Bms.BuildingBlocks.Core.Search;
using Bms.System.Application.Dtos;

namespace Bms.System.Application.Services;

/// <summary>
/// 全局搜索应用服务接口（系统业务数据）
/// </summary>
public interface IGlobalSearchAppService
{
    /// <summary>
    /// 全局搜索：用户名/姓名/手机号模糊匹配，仅返回有命中的分组
    /// </summary>
    /// <param name="request">搜索请求（关键字 + 每组条数上限）</param>
    Task<ApiResponseDto<List<SearchResultGroupDto>>> SearchAsync(GlobalSearchRequestDto request);
}

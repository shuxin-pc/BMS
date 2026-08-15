using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Activities;

namespace Bms.Store.Application.Services;

/// <summary>
/// 活动管理应用服务接口
/// </summary>
public interface IActivityAppService
{
    /// <summary>
    /// 获取活动分页列表
    /// </summary>
    Task<ApiResponseDto<PagedResponseDto<ActivityDto>>> GetPagedListAsync(ActivityQueryDto query);

    /// <summary>
    /// 根据ID获取活动详情
    /// </summary>
    Task<ApiResponseDto<ActivityDto?>> GetByIdAsync(long id);

    /// <summary>
    /// 创建活动
    /// </summary>
    Task<ApiResponseDto<ActivityDto>> CreateAsync(ActivityCreateDto dto);

    /// <summary>
    /// 更新活动
    /// </summary>
    Task<ApiResponseDto<ActivityDto>> UpdateAsync(ActivityUpdateDto dto);

    /// <summary>
    /// 删除活动（软删除）
    /// </summary>
    Task<ApiResponseDto> DeleteAsync(long id);

    /// <summary>
    /// 获取进行中活动下拉选项（供其他业务关联选择）
    /// </summary>
    Task<ApiResponseDto<List<ActivityOptionDto>>> GetOptionsAsync();
}

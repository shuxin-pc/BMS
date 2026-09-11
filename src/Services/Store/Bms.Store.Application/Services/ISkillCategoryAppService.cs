using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.SkillCategories;

namespace Bms.Store.Application.Services;

/// <summary>
/// 技能分类应用服务接口
/// </summary>
public interface ISkillCategoryAppService
{
    /// <summary>
    /// 获取技能分类分页列表
    /// </summary>
    Task<ApiResponseDto<PagedResponseDto<SkillCategoryDto>>> GetPagedListAsync(SkillCategoryQueryDto query);

    /// <summary>
    /// 根据ID获取技能分类详情
    /// </summary>
    Task<ApiResponseDto<SkillCategoryDto?>> GetByIdAsync(long id);

    /// <summary>
    /// 创建技能分类
    /// </summary>
    Task<ApiResponseDto<SkillCategoryDto>> CreateAsync(SkillCategoryCreateDto dto);

    /// <summary>
    /// 更新技能分类
    /// </summary>
    Task<ApiResponseDto<SkillCategoryDto>> UpdateAsync(SkillCategoryUpdateDto dto);

    /// <summary>
    /// 删除技能分类（级联软删除整棵子树，并物理清理技师/服务项目关联数据）
    /// </summary>
    Task<ApiResponseDto> DeleteAsync(long id);

    /// <summary>
    /// 批量删除技能分类（级联软删除整棵子树，并物理清理技师/服务项目关联数据）
    /// </summary>
    Task<ApiResponseDto> BatchDeleteAsync(List<long> ids);
}

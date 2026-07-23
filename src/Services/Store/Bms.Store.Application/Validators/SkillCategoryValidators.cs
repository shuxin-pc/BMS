using FluentValidation;
using Bms.Store.Application.Dtos.SkillCategories;

namespace Bms.Store.Application.Validators;

/// <summary>
/// 创建技能分类请求验证器
/// </summary>
public class SkillCategoryCreateDtoValidator : AbstractValidator<SkillCategoryCreateDto>
{
    public SkillCategoryCreateDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("名称不能为空")
            .MaximumLength(100).WithMessage("名称最多100个字符");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("编码不能为空")
            .MaximumLength(50).WithMessage("编码最多50个字符");

        RuleFor(x => x.Status)
            .Must(s => s == 0 || s == 1).WithMessage("状态只能为0(禁用)或1(启用)");
    }
}

/// <summary>
/// 更新技能分类请求验证器
/// </summary>
public class SkillCategoryUpdateDtoValidator : AbstractValidator<SkillCategoryUpdateDto>
{
    public SkillCategoryUpdateDtoValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("ID无效");

        Include(new SkillCategoryCreateDtoValidator());
    }
}

using FluentValidation;
using Bms.System.Application.Dtos.Subsystems;

namespace Bms.System.Application.Validators;

/// <summary>
/// 子系统创建 DTO 验证器
/// </summary>
public class SubsystemCreateDtoValidator : AbstractValidator<SubsystemCreateDto>
{
    public SubsystemCreateDtoValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("子系统编码不能为空")
            .MinimumLength(2).WithMessage("子系统编码至少2个字符")
            .MaximumLength(50).WithMessage("子系统编码最多50个字符")
            .Matches(@"^[a-zA-Z0-9_]+$").WithMessage("子系统编码只能包含字母、数字、下划线");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("子系统名称不能为空")
            .MaximumLength(100).WithMessage("子系统名称最多100个字符");

        RuleFor(x => x.Icon)
            .MaximumLength(100).WithMessage("图标最多100个字符");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("描述最多500个字符");

        RuleFor(x => x.Sort)
            .GreaterThanOrEqualTo(0).WithMessage("排序号不能小于0");

        RuleFor(x => x.Status)
            .InclusiveBetween(0, 1).WithMessage("状态只能是0或1");
    }
}

/// <summary>
/// 子系统更新 DTO 验证器
/// </summary>
public class SubsystemUpdateDtoValidator : AbstractValidator<SubsystemUpdateDto>
{
    public SubsystemUpdateDtoValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("子系统ID无效");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("子系统名称不能为空")
            .MaximumLength(100).WithMessage("子系统名称最多100个字符");

        RuleFor(x => x.Icon)
            .MaximumLength(100).WithMessage("图标最多100个字符");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("描述最多500个字符");

        RuleFor(x => x.Sort)
            .GreaterThanOrEqualTo(0).WithMessage("排序号不能小于0");

        RuleFor(x => x.Status)
            .InclusiveBetween(0, 1).WithMessage("状态只能是0或1");
    }
}
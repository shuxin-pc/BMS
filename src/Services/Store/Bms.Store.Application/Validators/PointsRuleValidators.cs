using FluentValidation;
using Bms.Store.Application.Dtos.PointsRules;

namespace Bms.Store.Application.Validators;

/// <summary>
/// 创建积分规则请求验证器
/// </summary>
public class PointsRuleCreateDtoValidator : AbstractValidator<PointsRuleCreateDto>
{
    public PointsRuleCreateDtoValidator()
    {
        RuleFor(x => x.PointsRate).GreaterThanOrEqualTo(0).WithMessage("积分比例必须大于等于0");
        RuleFor(x => x.DeductRate).GreaterThanOrEqualTo(0).WithMessage("抵扣比例必须大于等于0");
        RuleFor(x => x.MaxDeductAmount).GreaterThanOrEqualTo(0).WithMessage("最大抵扣金额必须大于等于0");
        RuleFor(x => x.Status).Must(s => s == 0 || s == 1).WithMessage("状态只能为0(禁用)或1(启用)");
    }
}

/// <summary>
/// 更新积分规则请求验证器
/// </summary>
public class PointsRuleUpdateDtoValidator : AbstractValidator<PointsRuleUpdateDto>
{
    public PointsRuleUpdateDtoValidator()
    {
        Include(new PointsRuleCreateDtoValidator());
    }
}

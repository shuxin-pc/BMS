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

        // 返利率失衡校验：PointsRate（每消费1元获得积分数）× DeductRate（每积分可抵扣金额）=
        // 返利率 c（消费1元返的积分折算成金额的比例，无量纲）
        // c ≥ 1 即"消费1元返的积分至少可抵扣1元"，门店倒贴，绝对失衡，禁止保存（与前端 returnRate >= 1 阻止逻辑一致）
        RuleFor(x => x.PointsRate * x.DeductRate)
            .LessThan(1m)
            .WithMessage("积分规则失衡：返利率已达或超过100%（消费1元返的积分可抵扣≥1元），门店将倒贴，请调低积分比例或提高抵扣所需积分");
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

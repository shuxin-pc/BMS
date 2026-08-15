using FluentValidation;
using Bms.Store.Application.Dtos.StoredValues;

namespace Bms.Store.Application.Validators;

/// <summary>
/// 创建储值规则请求验证器
/// </summary>
public class StoredValueRuleCreateDtoValidator : AbstractValidator<StoredValueRuleCreateDto>
{
    public StoredValueRuleCreateDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("规则名称不能为空")
            .MaximumLength(100).WithMessage("规则名称最多100个字符");

        // 充值档位金额参与"整倍叠加"拆分计算，为 0 会导致无法拆分，必须大于 0
        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("充值金额必须大于0");

        RuleFor(x => x.GiftAmount)
            .GreaterThanOrEqualTo(0).WithMessage("赠送金额必须大于等于0");

        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("生效日期不能为空");

        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate).WithMessage("失效日期不能早于生效日期")
            .When(x => x.EndDate.HasValue);
    }
}

/// <summary>
/// 更新储值规则请求验证器
/// </summary>
public class StoredValueRuleUpdateDtoValidator : AbstractValidator<StoredValueRuleUpdateDto>
{
    public StoredValueRuleUpdateDtoValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("ID无效");
        Include(new StoredValueRuleCreateDtoValidator());
    }
}

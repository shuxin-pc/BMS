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

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("规则编码不能为空")
            .MaximumLength(50).WithMessage("规则编码最多50个字符");

        RuleFor(x => x.Amount)
            .GreaterThanOrEqualTo(0).WithMessage("充值金额必须大于等于0");

        RuleFor(x => x.GiftAmount)
            .GreaterThanOrEqualTo(0).WithMessage("赠送金额必须大于等于0");
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

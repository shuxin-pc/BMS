using FluentValidation;
using Bms.Store.Application.Dtos.Customers;

namespace Bms.Store.Application.Validators;

public class CustomerLevelCreateDtoValidator : AbstractValidator<CustomerLevelCreateDto>
{
    public CustomerLevelCreateDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("等级名称不能为空")
            .MaximumLength(50).WithMessage("等级名称长度不能超过50");
        RuleFor(x => x.Code).NotEmpty().WithMessage("等级编码不能为空")
            .MaximumLength(50).WithMessage("等级编码长度不能超过50");
        // 等级值为正整数，同租户内唯一（唯一性由应用层校验）
        RuleFor(x => x.Level).GreaterThan(0).WithMessage("等级值必须为正整数");
        RuleFor(x => x.DiscountRate).InclusiveBetween(0m, 1m).WithMessage("折扣率取值范围为0-1");
    }
}

public class CustomerLevelUpdateDtoValidator : AbstractValidator<CustomerLevelUpdateDto>
{
    public CustomerLevelUpdateDtoValidator()
    {
        Include(new CustomerLevelCreateDtoValidator());
    }
}

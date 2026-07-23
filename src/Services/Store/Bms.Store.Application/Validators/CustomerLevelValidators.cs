using FluentValidation;
using Bms.Store.Application.Dtos.Customers;
using Bms.Store.Domain.Constants;

namespace Bms.Store.Application.Validators;

public class CustomerLevelCreateDtoValidator : AbstractValidator<CustomerLevelCreateDto>
{
    public CustomerLevelCreateDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("等级名称不能为空")
            .MaximumLength(50).WithMessage("等级名称长度不能超过50");
        RuleFor(x => x.Code).NotEmpty().WithMessage("等级编码不能为空")
            .MaximumLength(50).WithMessage("等级编码长度不能超过50");
        // 等级值必须为 1(普通会员) 或 2(会员)
        RuleFor(x => x.Level).Must(CustomerLevelTypes.IsValid)
            .WithMessage("等级值只能为 1(普通会员) 或 2(会员)");
        RuleFor(x => x.DiscountRate).InclusiveBetween(0m, 1m).WithMessage("折扣率取值范围为0-1");
    }
}

public class CustomerLevelUpdateDtoValidator : AbstractValidator<CustomerLevelUpdateDto>
{
    public CustomerLevelUpdateDtoValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("ID无效");
        Include(new CustomerLevelCreateDtoValidator());
    }
}

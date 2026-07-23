using FluentValidation;
using Bms.Store.Application.Dtos.Customers;

namespace Bms.Store.Application.Validators;

public class CustomerCreateDtoValidator : AbstractValidator<CustomerCreateDto>
{
    public CustomerCreateDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("客户名称不能为空")
            .MaximumLength(50).WithMessage("客户名称长度不能超过50");
        RuleFor(x => x.Phone).NotEmpty().WithMessage("手机号不能为空")
            .Matches(@"^1\d{10}$").WithMessage("手机号格式不正确");
        RuleFor(x => x.Gender).InclusiveBetween(0, 2).WithMessage("性别取值无效");
        RuleFor(x => x.AuthorizationStatus).InclusiveBetween(0, 2).WithMessage("授权状态取值无效");
    }
}

public class CustomerUpdateDtoValidator : AbstractValidator<CustomerUpdateDto>
{
    public CustomerUpdateDtoValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("ID无效");
        Include(new CustomerCreateDtoValidator());
    }
}

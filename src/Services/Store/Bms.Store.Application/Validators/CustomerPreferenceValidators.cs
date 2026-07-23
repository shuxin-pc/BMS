using FluentValidation;
using Bms.Store.Application.Dtos.Customers;

namespace Bms.Store.Application.Validators;

public class CustomerPreferenceCreateDtoValidator : AbstractValidator<CustomerPreferenceCreateDto>
{
    public CustomerPreferenceCreateDtoValidator()
    {
        RuleFor(x => x.CustomerId).GreaterThan(0).WithMessage("请选择客户");
    }
}

public class CustomerPreferenceUpdateDtoValidator : AbstractValidator<CustomerPreferenceUpdateDto>
{
    public CustomerPreferenceUpdateDtoValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("ID无效");
        Include(new CustomerPreferenceCreateDtoValidator());
    }
}

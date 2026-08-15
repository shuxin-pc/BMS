using FluentValidation;
using Bms.Store.Application.Dtos.Customers;

namespace Bms.Store.Application.Validators;

public class CustomerBeautyProfileCreateDtoValidator : AbstractValidator<CustomerBeautyProfileCreateDto>
{
    public CustomerBeautyProfileCreateDtoValidator()
    {
        RuleFor(x => x.CustomerId).GreaterThan(0).WithMessage("请选择客户");
    }
}

public class CustomerBeautyProfileUpdateDtoValidator : AbstractValidator<CustomerBeautyProfileUpdateDto>
{
    public CustomerBeautyProfileUpdateDtoValidator()
    {
        Include(new CustomerBeautyProfileCreateDtoValidator());
    }
}

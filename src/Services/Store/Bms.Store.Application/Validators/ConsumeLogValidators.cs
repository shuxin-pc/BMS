using FluentValidation;
using Bms.Store.Application.Dtos.Customers;

namespace Bms.Store.Application.Validators;

public class ConsumeLogCreateDtoValidator : AbstractValidator<ConsumeLogCreateDto>
{
    public ConsumeLogCreateDtoValidator()
    {
        RuleFor(x => x.CustomerId).GreaterThan(0).WithMessage("请选择客户");
        RuleFor(x => x.OrderId).GreaterThan(0).WithMessage("请选择订单");
        RuleFor(x => x.Amount).GreaterThanOrEqualTo(0).WithMessage("消费金额不能为负数");
    }
}

public class ConsumeLogUpdateDtoValidator : AbstractValidator<ConsumeLogUpdateDto>
{
    public ConsumeLogUpdateDtoValidator()
    {
        Include(new ConsumeLogCreateDtoValidator());
    }
}

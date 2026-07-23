using FluentValidation;
using Bms.Store.Application.Dtos.Customers;

namespace Bms.Store.Application.Validators;

public class ServiceReactionCreateDtoValidator : AbstractValidator<ServiceReactionCreateDto>
{
    public ServiceReactionCreateDtoValidator()
    {
        RuleFor(x => x.CustomerId).GreaterThan(0).WithMessage("请选择客户");
        RuleFor(x => x.Severity)
            .Must(s => !s.HasValue || (s >= 1 && s <= 3)).WithMessage("严重程度只能为1(轻微)、2(中等)或3(严重)");
    }
}

public class ServiceReactionUpdateDtoValidator : AbstractValidator<ServiceReactionUpdateDto>
{
    public ServiceReactionUpdateDtoValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("ID无效");
        Include(new ServiceReactionCreateDtoValidator());
    }
}

using FluentValidation;
using Bms.Store.Application.Dtos.Customers;

namespace Bms.Store.Application.Validators;

public class ServiceReactionCreateDtoValidator : AbstractValidator<ServiceReactionCreateDto>
{
    public ServiceReactionCreateDtoValidator()
    {
        RuleFor(x => x.CustomerId).GreaterThan(0).WithMessage("请选择客户");
        RuleFor(x => x.OrderId)
            .Must(id => !id.HasValue || id.Value > 0).WithMessage("关联订单不合法");
        RuleFor(x => x.ProductId)
            .Must(id => !id.HasValue || id.Value > 0).WithMessage("服务项目不合法");
        RuleFor(x => x.Severity)
            .Must(s => !s.HasValue || (s >= 1 && s <= 3)).WithMessage("严重程度只能为1(轻微)、2(中等)或3(严重)");
    }
}

public class ServiceReactionUpdateDtoValidator : AbstractValidator<ServiceReactionUpdateDto>
{
    public ServiceReactionUpdateDtoValidator()
    {
        Include(new ServiceReactionCreateDtoValidator());
    }
}

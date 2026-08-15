using FluentValidation;
using Bms.Store.Application.Dtos.ServiceBoms;

namespace Bms.Store.Application.Validators;

/// <summary>
/// 创建服务BOM请求验证器
/// </summary>
public class ServiceBomCreateDtoValidator : AbstractValidator<ServiceBomCreateDto>
{
    public ServiceBomCreateDtoValidator()
    {
        RuleFor(x => x.ServiceProductId)
            .GreaterThan(0).WithMessage("服务项目ID必须大于0");
        RuleFor(x => x.ConsumableProductId)
            .GreaterThan(0).WithMessage("耗材商品ID必须大于0");
        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("消耗数量必须大于0");
    }
}

/// <summary>
/// 更新服务BOM请求验证器
/// </summary>
public class ServiceBomUpdateDtoValidator : AbstractValidator<ServiceBomUpdateDto>
{
    public ServiceBomUpdateDtoValidator()
    {
        Include(new ServiceBomCreateDtoValidator());
    }
}

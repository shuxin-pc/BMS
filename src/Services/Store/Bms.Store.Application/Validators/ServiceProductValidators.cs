using FluentValidation;
using Bms.Store.Application.Dtos.Products;

namespace Bms.Store.Application.Validators;

/// <summary>
/// 创建服务商品子表请求验证器
/// </summary>
public class ServiceProductCreateDtoValidator : AbstractValidator<ServiceProductCreateDto>
{
    public ServiceProductCreateDtoValidator()
    {
        RuleFor(x => x.ProductId)
            .GreaterThan(0).WithMessage("商品ID必须大于0");
        RuleFor(x => x.Duration)
            .GreaterThan(0).When(x => x.Duration.HasValue).WithMessage("服务时长必须大于0");
        RuleFor(x => x.RequiredRoomType)
            .InclusiveBetween(1, 2).When(x => x.RequiredRoomType.HasValue).WithMessage("所需房间/床位类型必须为1（房间）或2（床位）");
    }
}

/// <summary>
/// 更新服务商品子表请求验证器
/// </summary>
public class ServiceProductUpdateDtoValidator : AbstractValidator<ServiceProductUpdateDto>
{
    public ServiceProductUpdateDtoValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("ID无效");
        Include(new ServiceProductCreateDtoValidator());
    }
}

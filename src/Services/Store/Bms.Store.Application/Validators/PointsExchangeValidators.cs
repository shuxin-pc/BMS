using FluentValidation;
using Bms.Store.Application.Dtos.Points;

namespace Bms.Store.Application.Validators;

/// <summary>
/// 创建积分兑换记录请求验证器
/// </summary>
public class PointsExchangeCreateDtoValidator : AbstractValidator<PointsExchangeCreateDto>
{
    public PointsExchangeCreateDtoValidator()
    {
        RuleFor(x => x.CustomerId)
            .GreaterThan(0).WithMessage("客户ID必须大于0");

        RuleFor(x => x.ExchangeType)
            .InclusiveBetween(1, 3).WithMessage("兑换类型必须是1(商品)、2(优惠券)或3(服务项目)");

        RuleFor(x => x.PointsCost)
            .GreaterThan(0).WithMessage("消耗积分必须大于0");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("兑换数量必须大于0");
    }
}

/// <summary>
/// 更新积分兑换记录请求验证器
/// </summary>
public class PointsExchangeUpdateDtoValidator : AbstractValidator<PointsExchangeUpdateDto>
{
    public PointsExchangeUpdateDtoValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("ID无效");
        Include(new PointsExchangeCreateDtoValidator());
    }
}

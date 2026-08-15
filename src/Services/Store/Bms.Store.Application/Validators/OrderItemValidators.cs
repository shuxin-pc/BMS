using FluentValidation;
using Bms.Store.Application.Dtos.Orders;

namespace Bms.Store.Application.Validators;

/// <summary>
/// 创建订单明细请求验证器
/// </summary>
public class OrderItemCreateDtoValidator : AbstractValidator<OrderItemCreateDto>
{
    public OrderItemCreateDtoValidator()
    {
        RuleFor(x => x.OrderId).GreaterThan(0).WithMessage("请选择订单");
        RuleFor(x => x.ProductId).GreaterThan(0).WithMessage("请选择商品");
        RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("数量必须大于0");
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0).WithMessage("单价不能小于0");
    }
}

/// <summary>
/// 更新订单明细请求验证器
/// </summary>
public class OrderItemUpdateDtoValidator : AbstractValidator<OrderItemUpdateDto>
{
    public OrderItemUpdateDtoValidator()
    {
        Include(new OrderItemCreateDtoValidator());
    }
}

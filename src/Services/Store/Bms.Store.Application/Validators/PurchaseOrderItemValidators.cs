using FluentValidation;
using Bms.Store.Application.Dtos.PurchaseOrders;

namespace Bms.Store.Application.Validators;

/// <summary>
/// 创建采购订单明细请求验证器
/// </summary>
public class PurchaseOrderItemCreateDtoValidator : AbstractValidator<PurchaseOrderItemCreateDto>
{
    public PurchaseOrderItemCreateDtoValidator()
    {
        RuleFor(x => x.PurchaseOrderId).GreaterThan(0).WithMessage("采购单ID必须大于0");
        RuleFor(x => x.ProductId).GreaterThan(0).WithMessage("商品ID必须大于0");
        RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("采购数量必须大于0");
        RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0).WithMessage("采购单价必须大于等于0");
        RuleFor(x => x.TotalPrice).GreaterThanOrEqualTo(0).WithMessage("小计金额必须大于等于0");
    }
}

/// <summary>
/// 更新采购订单明细请求验证器
/// </summary>
public class PurchaseOrderItemUpdateDtoValidator : AbstractValidator<PurchaseOrderItemUpdateDto>
{
    public PurchaseOrderItemUpdateDtoValidator()
    {
        Include(new PurchaseOrderItemCreateDtoValidator());
    }
}

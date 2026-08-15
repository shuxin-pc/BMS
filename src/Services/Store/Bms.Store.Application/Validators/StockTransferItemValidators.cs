using FluentValidation;
using Bms.Store.Application.Dtos.StockTransfers;

namespace Bms.Store.Application.Validators;

/// <summary>
/// 创建库存调拨单明细请求验证器
/// </summary>
public class StockTransferItemCreateDtoValidator : AbstractValidator<StockTransferItemCreateDto>
{
    public StockTransferItemCreateDtoValidator()
    {
        RuleFor(x => x.StockTransferId).GreaterThan(0).WithMessage("请选择调拨单");
        RuleFor(x => x.ProductId).GreaterThan(0).WithMessage("请选择商品");
        RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("调拨数量必须大于0");
    }
}

/// <summary>
/// 更新库存调拨单明细请求验证器
/// </summary>
public class StockTransferItemUpdateDtoValidator : AbstractValidator<StockTransferItemUpdateDto>
{
    public StockTransferItemUpdateDtoValidator()
    {
        Include(new StockTransferItemCreateDtoValidator());
    }
}

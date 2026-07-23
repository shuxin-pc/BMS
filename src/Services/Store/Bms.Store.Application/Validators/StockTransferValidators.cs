using FluentValidation;
using Bms.Store.Application.Dtos.StockTransfers;

namespace Bms.Store.Application.Validators;

/// <summary>
/// 创建库存调拨单请求验证器
/// Status 由 AppService 强制设置为待调出（草稿），不在此处校验
/// </summary>
public class StockTransferCreateDtoValidator : AbstractValidator<StockTransferCreateDto>
{
    public StockTransferCreateDtoValidator()
    {
        RuleFor(x => x.TransferNo).NotEmpty().WithMessage("调拨单号不能为空").MaximumLength(50).WithMessage("调拨单号最多50个字符");
        RuleFor(x => x.FromStoreId).GreaterThan(0).WithMessage("请选择调出门店");
        RuleFor(x => x.ToStoreId).GreaterThan(0).WithMessage("请选择调入门店");
        RuleFor(x => x.FromStoreId).NotEqual(x => x.ToStoreId).WithMessage("调出门店与调入门店不能相同");
    }
}

/// <summary>
/// 更新库存调拨单请求验证器
/// </summary>
public class StockTransferUpdateDtoValidator : AbstractValidator<StockTransferUpdateDto>
{
    public StockTransferUpdateDtoValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("ID无效");
        Include(new StockTransferCreateDtoValidator());
    }
}

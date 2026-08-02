using FluentValidation;
using Bms.Store.Application.Dtos.SampleGiftTransfers;

namespace Bms.Store.Application.Validators;

/// <summary>
/// 创建样品赠品调拨单请求验证器
/// TransferNo 由后端自动生成，不在此处校验
/// Status 由 AppService 强制设置为待调出，不在此处校验
/// 商品 Type∈{4,5} 校验 + 商品未删除校验在 AppService 内查库完成（Validator 不持 DbContext）
/// </summary>
public class SampleGiftTransferCreateDtoValidator : AbstractValidator<SampleGiftTransferCreateDto>
{
    public SampleGiftTransferCreateDtoValidator()
    {
        RuleFor(x => x.FromStoreId).GreaterThan(0).WithMessage("请选择调出门店");
        RuleFor(x => x.ToStoreId).GreaterThan(0).WithMessage("请选择调入门店");
        RuleFor(x => x.FromStoreId).NotEqual(x => x.ToStoreId).WithMessage("调出门店与调入门店不能相同");
        RuleFor(x => x.Items).NotNull().WithMessage("调拨明细不能为空")
            .Must(items => items != null && items.Count > 0).WithMessage("请至少添加一条调拨明细");
        // 级联创建时 SampleGiftTransferId 由后端按主单回填，此处仅校验商品与数量
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId).GreaterThan(0).WithMessage("请选择商品");
            item.RuleFor(i => i.Quantity).GreaterThan(0).WithMessage("调拨数量必须大于0");
        });
    }
}

/// <summary>
/// 更新样品赠品调拨单请求验证器
/// </summary>
public class SampleGiftTransferUpdateDtoValidator : AbstractValidator<SampleGiftTransferUpdateDto>
{
    public SampleGiftTransferUpdateDtoValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("ID无效");
        Include(new SampleGiftTransferCreateDtoValidator());
    }
}

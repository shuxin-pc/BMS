using FluentValidation;
using Bms.Store.Application.Dtos.SampleGifts;

namespace Bms.Store.Application.Validators;

/// <summary>
/// 创建赠品出库记录请求验证器
/// </summary>
public class SampleGiftOutCreateDtoValidator : AbstractValidator<SampleGiftOutCreateDto>
{
    public SampleGiftOutCreateDtoValidator()
    {
        RuleFor(x => x.ProductId)
            .GreaterThan(0).WithMessage("商品ID必须大于0");
        RuleFor(x => x.InventoryBatchId)
            .GreaterThan(0).WithMessage("出库批次ID必须大于0");
        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("出库数量必须大于0");
    }
}

/// <summary>
/// 更新赠品出库记录请求验证器
/// </summary>
public class SampleGiftOutUpdateDtoValidator : AbstractValidator<SampleGiftOutUpdateDto>
{
    public SampleGiftOutUpdateDtoValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("ID无效");
        Include(new SampleGiftOutCreateDtoValidator());
    }
}

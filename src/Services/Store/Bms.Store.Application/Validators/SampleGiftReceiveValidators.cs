using FluentValidation;
using Bms.Store.Application.Dtos.SampleGifts;

namespace Bms.Store.Application.Validators;

/// <summary>
/// 创建样品领用记录请求验证器
/// </summary>
public class SampleGiftReceiveCreateDtoValidator : AbstractValidator<SampleGiftReceiveCreateDto>
{
    public SampleGiftReceiveCreateDtoValidator()
    {
        RuleFor(x => x.ProductId)
            .GreaterThan(0).WithMessage("商品ID必须大于0");
        RuleFor(x => x.InventoryBatchId)
            .GreaterThan(0).WithMessage("出库批次ID必须大于0");
        // 客户可选：仅在传入值时校验有效性
        RuleFor(x => x.CustomerId)
            .GreaterThan(0).WithMessage("客户ID必须大于0")
            .When(x => x.CustomerId.HasValue);
        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("领取数量必须大于0");
    }
}

/// <summary>
/// 更新样品领用记录请求验证器
/// </summary>
public class SampleGiftReceiveUpdateDtoValidator : AbstractValidator<SampleGiftReceiveUpdateDto>
{
    public SampleGiftReceiveUpdateDtoValidator()
    {
        Include(new SampleGiftReceiveCreateDtoValidator());
    }
}

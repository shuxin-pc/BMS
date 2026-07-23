using FluentValidation;
using Bms.Store.Application.Dtos.PriceChangeLogs;

namespace Bms.Store.Application.Validators;

/// <summary>
/// 创建价格变更记录请求验证器
/// </summary>
public class PriceChangeLogCreateDtoValidator : AbstractValidator<PriceChangeLogCreateDto>
{
    public PriceChangeLogCreateDtoValidator()
    {
        RuleFor(x => x.ProductId).GreaterThan(0).WithMessage("请选择商品");
        RuleFor(x => x.OldPrice).GreaterThanOrEqualTo(0).WithMessage("原价格不能小于0");
        RuleFor(x => x.NewPrice).GreaterThanOrEqualTo(0).WithMessage("新价格不能小于0");
    }
}

/// <summary>
/// 更新价格变更记录请求验证器
/// </summary>
public class PriceChangeLogUpdateDtoValidator : AbstractValidator<PriceChangeLogUpdateDto>
{
    public PriceChangeLogUpdateDtoValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("ID无效");
        Include(new PriceChangeLogCreateDtoValidator());
    }
}

/// <summary>
/// 批量调价请求验证器
/// 按 RangeType 校验对应范围字段必填，按 AdjustType 校验调价值合法性
/// </summary>
public class BatchPriceAdjustDtoValidator : AbstractValidator<BatchPriceAdjustDto>
{
    public BatchPriceAdjustDtoValidator()
    {
        RuleFor(x => x.RangeType)
            .InclusiveBetween(1, 4)
            .WithMessage("调价范围类型无效（1=商品ID列表 2=商品分类 3=供应商 4=全部商品）");

        // RangeType=1 时必须提供商品ID列表
        RuleFor(x => x.ProductIds)
            .Must(p => p != null && p.Any())
            .When(x => x.RangeType == 1)
            .WithMessage("RangeType=1 时必须提供商品ID列表");

        // RangeType=2 时必须提供有效的商品分类ID
        RuleFor(x => x.ProductCategoryId)
            .NotNull()
            .GreaterThan(0)
            .When(x => x.RangeType == 2)
            .WithMessage("RangeType=2 时必须提供有效的商品分类ID");

        // RangeType=3 时必须提供有效的供应商ID
        RuleFor(x => x.SupplierId)
            .NotNull()
            .GreaterThan(0)
            .When(x => x.RangeType == 3)
            .WithMessage("RangeType=3 时必须提供有效的供应商ID");

        RuleFor(x => x.AdjustType)
            .InclusiveBetween(1, 3)
            .WithMessage("调价方式无效（1=百分比 2=固定金额增减 3=设置为新值）");

        // AdjustType=3（设置为新值）时 AdjustValue 必须 >= 0
        RuleFor(x => x.AdjustValue)
            .GreaterThanOrEqualTo(0)
            .When(x => x.AdjustType == 3)
            .WithMessage("设置为新值时，新价格不能为负数");
    }
}

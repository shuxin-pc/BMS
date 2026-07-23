using FluentValidation;
using Bms.Store.Application.Dtos.Statistics;

namespace Bms.Store.Application.Validators;

/// <summary>
/// 创建商品销售统计请求验证器
/// </summary>
public class ProductSalesStatCreateDtoValidator : AbstractValidator<ProductSalesStatCreateDto>
{
    public ProductSalesStatCreateDtoValidator()
    {
        RuleFor(x => x.ProductId).GreaterThan(0).WithMessage("请选择商品");
        RuleFor(x => x.StatMonth).NotEmpty().WithMessage("统计月份不能为空").MaximumLength(7).WithMessage("统计月份格式应为yyyy-MM");
        RuleFor(x => x.SalesCount).GreaterThanOrEqualTo(0).WithMessage("销售次数必须大于等于0");
        RuleFor(x => x.SalesAmount).GreaterThanOrEqualTo(0).WithMessage("销售金额必须大于等于0");
    }
}

/// <summary>
/// 更新商品销售统计请求验证器
/// </summary>
public class ProductSalesStatUpdateDtoValidator : AbstractValidator<ProductSalesStatUpdateDto>
{
    public ProductSalesStatUpdateDtoValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("ID无效");
        Include(new ProductSalesStatCreateDtoValidator());
    }
}

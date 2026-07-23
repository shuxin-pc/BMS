using FluentValidation;
using Bms.Store.Application.Dtos.Products;

namespace Bms.Store.Application.Validators;

/// <summary>
/// 创建商品请求验证器（规则对齐前端表单校验）
/// </summary>
public class ProductCreateDtoValidator : AbstractValidator<ProductCreateDto>
{
    public ProductCreateDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("商品名称不能为空")
            .MaximumLength(100).WithMessage("商品名称最多100个字符");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("商品编码不能为空")
            .MinimumLength(2).WithMessage("商品编码长度为2-50个字符")
            .MaximumLength(50).WithMessage("商品编码长度为2-50个字符")
            .Matches(@"^[a-zA-Z0-9_]+$").WithMessage("商品编码只能包含字母、数字、下划线");

        RuleFor(x => x.CategoryId)
            .GreaterThan(0).WithMessage("请选择商品分类");

        RuleFor(x => x.Type)
            .Must(t => t == 1 || t == 2).WithMessage("商品类型只能为1(实物商品)或2(服务项目)");

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0).WithMessage("售价必须大于等于0");

        RuleFor(x => x.Status)
            .Must(s => s == 1 || s == 2).WithMessage("商品状态只能为1(上架)或2(下架)");
    }
}

/// <summary>
/// 更新商品请求验证器
/// </summary>
public class ProductUpdateDtoValidator : AbstractValidator<ProductUpdateDto>
{
    public ProductUpdateDtoValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("商品ID无效");

        Include(new ProductCreateDtoValidator());
    }
}

/// <summary>
/// 创建商品分类请求验证器（规则对齐前端表单校验）
/// </summary>
public class ProductCategoryCreateDtoValidator : AbstractValidator<ProductCategoryCreateDto>
{
    public ProductCategoryCreateDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("分类名称不能为空")
            .MaximumLength(50).WithMessage("分类名称最多50个字符");
    }
}

/// <summary>
/// 更新商品分类请求验证器
/// </summary>
public class ProductCategoryUpdateDtoValidator : AbstractValidator<ProductCategoryUpdateDto>
{
    public ProductCategoryUpdateDtoValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("分类ID无效");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("分类名称不能为空")
            .MaximumLength(50).WithMessage("分类名称最多50个字符");
    }
}

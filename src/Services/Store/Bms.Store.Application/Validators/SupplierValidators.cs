using FluentValidation;
using Bms.Store.Application.Dtos.Suppliers;

namespace Bms.Store.Application.Validators;

/// <summary>
/// 创建供应商请求验证器
/// </summary>
public class SupplierCreateDtoValidator : AbstractValidator<SupplierCreateDto>
{
    public SupplierCreateDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("供应商名称不能为空")
            .MaximumLength(100).WithMessage("供应商名称最多100个字符");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("供应商编码不能为空")
            .MaximumLength(50).WithMessage("供应商编码最多50个字符");

        RuleFor(x => x.Status)
            .InclusiveBetween(0, 1).WithMessage("状态只能为0(禁用)或1(启用)");
    }
}

/// <summary>
/// 更新供应商请求验证器
/// </summary>
public class SupplierUpdateDtoValidator : AbstractValidator<SupplierUpdateDto>
{
    public SupplierUpdateDtoValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("ID无效");

        Include(new SupplierCreateDtoValidator());
    }
}

/// <summary>
/// 绑定品项到供应商请求验证器
/// </summary>
public class BindProductsDtoValidator : AbstractValidator<BindProductsDto>
{
    public BindProductsDtoValidator()
    {
        RuleFor(x => x.SupplierId).GreaterThan(0).WithMessage("供应商ID必须大于0");
        RuleFor(x => x.ProductIds).NotEmpty().WithMessage("待绑定的品项ID列表不能为空");
        RuleForEach(x => x.ProductIds).GreaterThan(0).WithMessage("品项ID必须大于0");
    }
}

/// <summary>
/// 设置默认供应商请求验证器
/// </summary>
public class SetDefaultSupplierDtoValidator : AbstractValidator<SetDefaultSupplierDto>
{
    public SetDefaultSupplierDtoValidator()
    {
        RuleFor(x => x.ProductId).GreaterThan(0).WithMessage("商品ID必须大于0");
        RuleFor(x => x.SupplierId).GreaterThan(0).WithMessage("供应商ID必须大于0");
        RuleFor(x => x.ReferencePrice)
            .GreaterThanOrEqualTo(0).When(x => x.ReferencePrice.HasValue)
            .WithMessage("参考采购价必须大于等于0");
        RuleFor(x => x.LeadTimeDays)
            .GreaterThanOrEqualTo(0).When(x => x.LeadTimeDays.HasValue)
            .WithMessage("供货周期必须大于等于0");
    }
}

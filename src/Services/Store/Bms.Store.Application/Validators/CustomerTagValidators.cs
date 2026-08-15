using FluentValidation;
using Bms.Store.Application.Dtos.Customers;

namespace Bms.Store.Application.Validators;

/// <summary>
/// 创建客户标签校验器
/// </summary>
public class CustomerTagCreateDtoValidator : AbstractValidator<CustomerTagCreateDto>
{
    private static readonly string[] ValidColors = { "primary", "success", "warning", "danger", "info" };

    public CustomerTagCreateDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("标签名称不能为空")
            .MaximumLength(50).WithMessage("标签名称长度不能超过50");
        RuleFor(x => x.Color).Must(c => string.IsNullOrEmpty(c) || ValidColors.Contains(c))
            .WithMessage("标签颜色只能为 primary/success/warning/danger/info 之一");
        RuleFor(x => x.Remark).MaximumLength(500).When(x => !string.IsNullOrEmpty(x.Remark))
            .WithMessage("备注长度不能超过500");
    }
}

/// <summary>
/// 更新客户标签校验器
/// </summary>
public class CustomerTagUpdateDtoValidator : AbstractValidator<CustomerTagUpdateDto>
{
    public CustomerTagUpdateDtoValidator()
    {
        Include(new CustomerTagCreateDtoValidator());
    }
}

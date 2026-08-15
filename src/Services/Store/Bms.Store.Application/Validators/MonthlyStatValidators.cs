using FluentValidation;
using Bms.Store.Application.Dtos.Statistics;

namespace Bms.Store.Application.Validators;

/// <summary>
/// 创建月统计请求验证器
/// </summary>
public class MonthlyStatCreateDtoValidator : AbstractValidator<MonthlyStatCreateDto>
{
    public MonthlyStatCreateDtoValidator()
    {
        RuleFor(x => x.StatMonth).NotEmpty().WithMessage("统计月份不能为空").MaximumLength(7).WithMessage("统计月份格式应为yyyy-MM");
        RuleFor(x => x.OrderCount).GreaterThanOrEqualTo(0).WithMessage("订单数量必须大于等于0");
        RuleFor(x => x.Revenue).GreaterThanOrEqualTo(0).WithMessage("营收必须大于等于0");
        RuleFor(x => x.Cost).GreaterThanOrEqualTo(0).WithMessage("成本必须大于等于0");
        RuleFor(x => x.RefundAmount).GreaterThanOrEqualTo(0).WithMessage("退款金额必须大于等于0");
    }
}

/// <summary>
/// 更新月统计请求验证器
/// </summary>
public class MonthlyStatUpdateDtoValidator : AbstractValidator<MonthlyStatUpdateDto>
{
    public MonthlyStatUpdateDtoValidator()
    {
        Include(new MonthlyStatCreateDtoValidator());
    }
}

using FluentValidation;
using Bms.Store.Application.Dtos.DailySettlements;

namespace Bms.Store.Application.Validators;

/// <summary>
/// 手动汇总日结请求验证器
/// </summary>
public class ManualSummarizeRequestDtoValidator : AbstractValidator<ManualSummarizeRequestDto>
{
    public ManualSummarizeRequestDtoValidator()
    {
        RuleFor(x => x.Date)
            .Must(d => !d.HasValue || d.Value.Date <= DateTime.Today)
            .WithMessage("汇总日期不能是未来日期");

        RuleFor(x => x.Remark)
            .MaximumLength(500).WithMessage("备注最大长度为500");
    }
}

/// <summary>
/// 反日结请求验证器
/// </summary>
public class ReverseRequestDtoValidator : AbstractValidator<ReverseRequestDto>
{
    public ReverseRequestDtoValidator()
    {
        RuleFor(x => x.Reason)
            .MaximumLength(500).WithMessage("反日结原因最大长度为500");
    }
}

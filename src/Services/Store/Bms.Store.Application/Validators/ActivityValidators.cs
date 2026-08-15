using FluentValidation;
using Bms.Store.Application.Dtos.Activities;

namespace Bms.Store.Application.Validators;

/// <summary>
/// 创建活动请求验证器
/// </summary>
public class ActivityCreateDtoValidator : AbstractValidator<ActivityCreateDto>
{
    public ActivityCreateDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("活动名称不能为空")
            .MaximumLength(100).WithMessage("活动名称最多100个字符");

        RuleFor(x => x.EndTime)
            .GreaterThanOrEqualTo(x => x.StartTime).WithMessage("结束时间不能早于开始时间");

        RuleFor(x => x.Remark)
            .MaximumLength(500).When(x => !string.IsNullOrWhiteSpace(x.Remark))
            .WithMessage("备注最多500个字符");
    }
}

/// <summary>
/// 更新活动请求验证器
/// </summary>
public class ActivityUpdateDtoValidator : AbstractValidator<ActivityUpdateDto>
{
    public ActivityUpdateDtoValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("ID无效");

        Include(new ActivityCreateDtoValidator());
    }
}

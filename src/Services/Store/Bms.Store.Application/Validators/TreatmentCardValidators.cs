using FluentValidation;
using Bms.Store.Application.Dtos.TreatmentCards;

namespace Bms.Store.Application.Validators;

/// <summary>
/// 创建疗程卡配置请求验证器
/// </summary>
public class TreatmentCardCreateDtoValidator : AbstractValidator<TreatmentCardCreateDto>
{
    public TreatmentCardCreateDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("卡名称不能为空")
            .MaximumLength(100).WithMessage("卡名称最多100个字符");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("卡编码不能为空")
            .MaximumLength(50).WithMessage("卡编码最多50个字符");

        RuleFor(x => x.TotalTimes)
            .GreaterThan(0).WithMessage("总次数必须大于0");

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0).WithMessage("单价必须大于等于0");

        RuleFor(x => x.ValidityDays)
            .GreaterThan(0).WithMessage("有效期必须大于0天");
    }
}

/// <summary>
/// 更新疗程卡配置请求验证器
/// </summary>
public class TreatmentCardUpdateDtoValidator : AbstractValidator<TreatmentCardUpdateDto>
{
    public TreatmentCardUpdateDtoValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("ID无效");
        Include(new TreatmentCardCreateDtoValidator());
    }
}

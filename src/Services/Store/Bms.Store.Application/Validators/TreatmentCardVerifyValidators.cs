using FluentValidation;
using Bms.Store.Application.Dtos.TreatmentCards;

namespace Bms.Store.Application.Validators;

/// <summary>
/// 创建项目卡核销记录请求验证器
/// </summary>
public class TreatmentCardVerifyCreateDtoValidator : AbstractValidator<TreatmentCardVerifyCreateDto>
{
    public TreatmentCardVerifyCreateDtoValidator()
    {
        RuleFor(x => x.CardSaleId)
            .GreaterThan(0).WithMessage("项目卡销售记录ID必须大于0");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("核销项目列表不能为空，至少包含 1 项");

        RuleForEach(x => x.Items).SetValidator(new TreatmentCardVerifyItemInputValidator());
    }
}

/// <summary>
/// 核销项目输入验证器
/// </summary>
public class TreatmentCardVerifyItemInputValidator : AbstractValidator<TreatmentCardVerifyItemInput>
{
    public TreatmentCardVerifyItemInputValidator()
    {
        RuleFor(x => x.ProductId)
            .GreaterThan(0).WithMessage("核销项目ID必须大于0");

        RuleFor(x => x.VerifyTimes)
            .GreaterThanOrEqualTo(1).WithMessage("核销次数必须 >= 1");
    }
}

/// <summary>
/// 更新项目卡核销记录请求验证器
/// </summary>
public class TreatmentCardVerifyUpdateDtoValidator : AbstractValidator<TreatmentCardVerifyUpdateDto>
{
    public TreatmentCardVerifyUpdateDtoValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("ID无效");

        Include(new TreatmentCardVerifyCreateDtoValidator());
    }
}

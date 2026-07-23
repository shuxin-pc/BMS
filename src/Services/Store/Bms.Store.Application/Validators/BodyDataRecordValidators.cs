using FluentValidation;
using Bms.Store.Application.Dtos.Customers;

namespace Bms.Store.Application.Validators;

public class BodyDataRecordCreateDtoValidator : AbstractValidator<BodyDataRecordCreateDto>
{
    public BodyDataRecordCreateDtoValidator()
    {
        RuleFor(x => x.CustomerId).GreaterThan(0).WithMessage("请选择客户");
        RuleFor(x => x.Weight).GreaterThanOrEqualTo(0).When(x => x.Weight.HasValue).WithMessage("体重必须大于等于0");
        RuleFor(x => x.BodyFat).GreaterThanOrEqualTo(0).When(x => x.BodyFat.HasValue).WithMessage("体脂率必须大于等于0");
        RuleFor(x => x.Bust).GreaterThanOrEqualTo(0).When(x => x.Bust.HasValue).WithMessage("胸围必须大于等于0");
        RuleFor(x => x.Waist).GreaterThanOrEqualTo(0).When(x => x.Waist.HasValue).WithMessage("腰围必须大于等于0");
        RuleFor(x => x.Hip).GreaterThanOrEqualTo(0).When(x => x.Hip.HasValue).WithMessage("臀围必须大于等于0");
    }
}

public class BodyDataRecordUpdateDtoValidator : AbstractValidator<BodyDataRecordUpdateDto>
{
    public BodyDataRecordUpdateDtoValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("ID无效");
        Include(new BodyDataRecordCreateDtoValidator());
    }
}

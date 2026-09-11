using FluentValidation;
using Bms.Store.Application.Dtos.TreatmentCards;

namespace Bms.Store.Application.Validators;

/// <summary>
/// 创建项目卡项目明细请求验证器
/// </summary>
public class CourseCardItemCreateDtoValidator : AbstractValidator<CourseCardItemCreateDto>
{
    public CourseCardItemCreateDtoValidator()
    {
        RuleFor(x => x.ProductId)
            .GreaterThan(0).WithMessage("商品ID必须大于0");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("项目次数必须大于0");

        RuleFor(x => x.OriginalPrice)
            .GreaterThanOrEqualTo(0).WithMessage("项目原价必须大于等于0");
    }
}

/// <summary>
/// 更新项目卡项目关联请求验证器
/// </summary>
public class CourseCardItemUpdateDtoValidator : AbstractValidator<CourseCardItemUpdateDto>
{
    public CourseCardItemUpdateDtoValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("ID无效");

        Include(new CourseCardItemCreateDtoValidator());
    }
}

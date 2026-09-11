using FluentValidation;
using Bms.Store.Application.Dtos.TreatmentCards;

namespace Bms.Store.Application.Validators;

/// <summary>
/// 创建项目卡销售记录请求验证器
/// </summary>
public class TreatmentCardSaleCreateDtoValidator : AbstractValidator<TreatmentCardSaleCreateDto>
{
    public TreatmentCardSaleCreateDtoValidator()
    {
        RuleFor(x => x.CardId)
            .GreaterThan(0).WithMessage("项目卡ID必须大于0");

        RuleFor(x => x.CustomerId)
            .GreaterThan(0).WithMessage("客户ID必须大于0");

        RuleFor(x => x.Amount)
            .GreaterThanOrEqualTo(0).WithMessage("购买金额必须大于等于0");

        RuleFor(x => x.Status)
            .InclusiveBetween(1, 3).WithMessage("状态只能为1(有效)、2(已用完)或3(已过期)");
    }
}

/// <summary>
/// 更新项目卡销售记录请求验证器
/// </summary>
public class TreatmentCardSaleUpdateDtoValidator : AbstractValidator<TreatmentCardSaleUpdateDto>
{
    public TreatmentCardSaleUpdateDtoValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("ID无效");

        Include(new TreatmentCardSaleCreateDtoValidator());
    }
}

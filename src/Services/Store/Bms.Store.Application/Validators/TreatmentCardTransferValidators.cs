using FluentValidation;
using Bms.Store.Application.Dtos.TreatmentCardTransfers;

namespace Bms.Store.Application.Validators;

/// <summary>
/// 创建疗程卡转让请求验证器
/// </summary>
public class TreatmentCardTransferCreateDtoValidator : AbstractValidator<TreatmentCardTransferCreateDto>
{
    public TreatmentCardTransferCreateDtoValidator()
    {
        RuleFor(x => x.CardSaleId)
            .GreaterThan(0).WithMessage("疗程卡销售记录ID必须大于0");

        RuleFor(x => x.FromCustomerId)
            .GreaterThan(0).WithMessage("原客户ID必须大于0");

        RuleFor(x => x.ToCustomerId)
            .GreaterThan(0).WithMessage("新客户ID必须大于0");

        RuleFor(x => x.TransferFee)
            .GreaterThanOrEqualTo(0).WithMessage("转让手续费必须大于等于0");

        RuleFor(x => x.Status)
            .Must(s => s == 0 || s == 1).WithMessage("状态只能为0(待转让)或1(已转让)");
    }
}

/// <summary>
/// 更新疗程卡转让请求验证器
/// </summary>
public class TreatmentCardTransferUpdateDtoValidator : AbstractValidator<TreatmentCardTransferUpdateDto>
{
    public TreatmentCardTransferUpdateDtoValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("ID无效");

        Include(new TreatmentCardTransferCreateDtoValidator());
    }
}

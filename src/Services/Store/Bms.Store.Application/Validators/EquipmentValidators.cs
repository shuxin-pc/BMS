using FluentValidation;
using Bms.Store.Application.Dtos.Equipments;
using Bms.Store.Domain.Constants;

namespace Bms.Store.Application.Validators;

/// <summary>
/// 创建设备台账请求验证器
/// </summary>
public class EquipmentCreateDtoValidator : AbstractValidator<EquipmentCreateDto>
{
    public EquipmentCreateDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("设备名称不能为空")
            .MaximumLength(100).WithMessage("设备名称最多100个字符");
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("设备编码不能为空")
            .MaximumLength(50).WithMessage("设备编码最多50个字符");
        RuleFor(x => x.Status)
            .Must(EquipmentStatus.IsValid)
            .WithMessage("状态只能为1(正常)、2(维修中)或3(已停用)");
        RuleFor(x => x.PurchasePrice)
            .GreaterThanOrEqualTo(0).WithMessage("购买价格必须大于等于0")
            .When(x => x.PurchasePrice.HasValue);
        RuleFor(x => x.NextMaintenanceDate)
            .GreaterThan(x => x.LastMaintenanceDate)
            .WithMessage("下次保养日期必须晚于上次保养日期")
            .When(x => x.LastMaintenanceDate.HasValue && x.NextMaintenanceDate.HasValue);
    }
}

/// <summary>
/// 更新设备台账请求验证器
/// </summary>
public class EquipmentUpdateDtoValidator : AbstractValidator<EquipmentUpdateDto>
{
    public EquipmentUpdateDtoValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("ID无效");
        Include(new EquipmentCreateDtoValidator());
    }
}

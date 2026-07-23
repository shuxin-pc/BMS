using FluentValidation;
using Bms.Store.Application.Dtos.Equipments;

namespace Bms.Store.Application.Validators;

/// <summary>
/// 创建设备维护记录请求验证器
/// </summary>
public class EquipmentMaintenanceCreateDtoValidator : AbstractValidator<EquipmentMaintenanceCreateDto>
{
    public EquipmentMaintenanceCreateDtoValidator()
    {
        RuleFor(x => x.EquipmentId).GreaterThan(0).WithMessage("设备ID必须大于0");
        RuleFor(x => x.MaintenanceType).Must(t => t == 1 || t == 2 || t == 3).WithMessage("维护类型只能为1(日常保养)、2(定期保养)或3(维修)");
        RuleFor(x => x.MaintenanceDate).NotEmpty().WithMessage("维护日期不能为空");
        RuleFor(x => x.Cost).GreaterThanOrEqualTo(0).When(x => x.Cost.HasValue).WithMessage("维护费用必须大于等于0");
    }
}

/// <summary>
/// 更新设备维护记录请求验证器
/// </summary>
public class EquipmentMaintenanceUpdateDtoValidator : AbstractValidator<EquipmentMaintenanceUpdateDto>
{
    public EquipmentMaintenanceUpdateDtoValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("ID无效");
        Include(new EquipmentMaintenanceCreateDtoValidator());
    }
}

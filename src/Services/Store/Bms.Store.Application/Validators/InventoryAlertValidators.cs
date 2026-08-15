using FluentValidation;
using Bms.Store.Application.Dtos.Inventories;

namespace Bms.Store.Application.Validators;

/// <summary>
/// 创建库存预警请求验证器
/// </summary>
public class InventoryAlertCreateDtoValidator : AbstractValidator<InventoryAlertCreateDto>
{
    public InventoryAlertCreateDtoValidator()
    {
        RuleFor(x => x.ProductId)
            .GreaterThan(0).WithMessage("关联商品ID必须大于0");
        RuleFor(x => x.AlertType)
            .Must(t => t >= 1 && t <= 3).WithMessage("预警类型只能为1(库存不足)、2(效期预警)或3(积压预警)");
    }
}

/// <summary>
/// 更新库存预警请求验证器
/// </summary>
public class InventoryAlertUpdateDtoValidator : AbstractValidator<InventoryAlertUpdateDto>
{
    public InventoryAlertUpdateDtoValidator()
    {
        Include(new InventoryAlertCreateDtoValidator());
    }
}

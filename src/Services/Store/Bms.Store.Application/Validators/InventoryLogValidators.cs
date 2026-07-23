using FluentValidation;
using Bms.Store.Application.Dtos.Inventories;

namespace Bms.Store.Application.Validators;

/// <summary>
/// 创建库存流水请求验证器
/// </summary>
public class InventoryLogCreateDtoValidator : AbstractValidator<InventoryLogCreateDto>
{
    public InventoryLogCreateDtoValidator()
    {
        RuleFor(x => x.ProductId)
            .GreaterThan(0).WithMessage("关联商品ID必须大于0");
        RuleFor(x => x.Type)
            .Must(t => t >= 1 && t <= 4).WithMessage("流水类型只能为1(入库)、2(出库)、3(盘点)或4(调拨)");
    }
}

/// <summary>
/// 更新库存流水请求验证器
/// </summary>
public class InventoryLogUpdateDtoValidator : AbstractValidator<InventoryLogUpdateDto>
{
    public InventoryLogUpdateDtoValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("ID无效");
        Include(new InventoryLogCreateDtoValidator());
    }
}

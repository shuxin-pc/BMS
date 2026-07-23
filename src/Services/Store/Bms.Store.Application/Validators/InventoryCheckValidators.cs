using FluentValidation;
using Bms.Store.Application.Dtos.Inventories;

namespace Bms.Store.Application.Validators;

/// <summary>
/// 创建库存盘点记录请求验证器
/// </summary>
public class InventoryCheckCreateDtoValidator : AbstractValidator<InventoryCheckCreateDto>
{
    public InventoryCheckCreateDtoValidator()
    {
        RuleFor(x => x.ProductId)
            .GreaterThan(0).WithMessage("关联商品ID必须大于0");
    }
}

/// <summary>
/// 更新库存盘点记录请求验证器
/// </summary>
public class InventoryCheckUpdateDtoValidator : AbstractValidator<InventoryCheckUpdateDto>
{
    public InventoryCheckUpdateDtoValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("ID无效");
        Include(new InventoryCheckCreateDtoValidator());
    }
}

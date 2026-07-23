using FluentValidation;
using Bms.Store.Application.Dtos.Inventories;

namespace Bms.Store.Application.Validators;

/// <summary>
/// 创建库存请求验证器
/// </summary>
public class InventoryCreateDtoValidator : AbstractValidator<InventoryCreateDto>
{
    public InventoryCreateDtoValidator()
    {
        RuleFor(x => x.ProductId)
            .GreaterThan(0).WithMessage("关联商品ID必须大于0");
    }
}

/// <summary>
/// 更新库存请求验证器
/// </summary>
public class InventoryUpdateDtoValidator : AbstractValidator<InventoryUpdateDto>
{
    public InventoryUpdateDtoValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("ID无效");
        Include(new InventoryCreateDtoValidator());
    }
}

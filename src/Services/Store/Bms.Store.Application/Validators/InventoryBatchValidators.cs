using FluentValidation;
using Bms.Store.Application.Dtos.InventoryBatches;

namespace Bms.Store.Application.Validators;

/// <summary>
/// 创建库存批次请求验证器
/// </summary>
public class InventoryBatchCreateDtoValidator : AbstractValidator<InventoryBatchCreateDto>
{
    public InventoryBatchCreateDtoValidator()
    {
        RuleFor(x => x.ProductId).GreaterThan(0).WithMessage("请选择商品");
        RuleFor(x => x.BatchNo).NotEmpty().WithMessage("批次号不能为空").MaximumLength(50).WithMessage("批次号最多50个字符");
        RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("数量必须大于0");
        RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0).WithMessage("单价必须大于等于0");
        RuleFor(x => x.Status).Must(s => s == 1 || s == 2 || s == 3).WithMessage("状态只能为1(在库)、2(已用完)或3(已过期)");
        // 到期日期可选：未填到期日期的批次视为"无效期限制"批次，不参与效期预警与效期销售统计
    }
}

/// <summary>
/// 更新库存批次请求验证器
/// </summary>
public class InventoryBatchUpdateDtoValidator : AbstractValidator<InventoryBatchUpdateDto>
{
    public InventoryBatchUpdateDtoValidator()
    {
        Include(new InventoryBatchCreateDtoValidator());
    }
}

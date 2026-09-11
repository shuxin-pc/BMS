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
        Include(new InventoryCheckCreateDtoValidator());
    }
}

/// <summary>
/// 提交盘点单请求验证器
/// 仅校验 DTO 字段级规则，跨字段业务规则（如盘盈必填原因）由服务层按差异方向判断后校验
/// </summary>
public class SubmitCheckDtoValidator : AbstractValidator<SubmitCheckDto>
{
    public SubmitCheckDtoValidator()
    {
        RuleFor(x => x.ActualQuantity).GreaterThanOrEqualTo(0).WithMessage("实际数量不能为负数");

        // 盘亏批次扣减明细校验（若提供）
        RuleForEach(x => x.DeductBatches).ChildRules(item =>
        {
            item.RuleFor(i => i.BatchId).GreaterThan(0).WithMessage("批次ID必须大于0");
            item.RuleFor(i => i.Quantity).GreaterThan(0).WithMessage("扣减数量必须大于0");
        });

        // 不允许重复批次ID
        RuleFor(x => x.DeductBatches)
            .Must(list => list == null || list.Select(i => i.BatchId).Distinct().Count() == list.Count)
            .WithMessage("扣减明细中存在重复的批次ID");

        // 盘盈批次累加明细校验（若提供）
        RuleForEach(x => x.GainBatches).ChildRules(item =>
        {
            item.RuleFor(i => i.BatchId).GreaterThan(0).WithMessage("批次ID必须大于0");
            item.RuleFor(i => i.Quantity).GreaterThan(0).WithMessage("累加数量必须大于0");
        });

        // 不允许重复批次ID
        RuleFor(x => x.GainBatches)
            .Must(list => list == null || list.Select(i => i.BatchId).Distinct().Count() == list.Count)
            .WithMessage("盘盈累加明细中存在重复的批次ID");

        // 盘盈单价校验（若提供）
        RuleFor(x => x.GainUnitPrice)
            .GreaterThanOrEqualTo(0).When(x => x.GainUnitPrice.HasValue)
            .WithMessage("盘盈单价不能为负数");

        // 盘盈过期日期必须晚于生产日期（若同时提供）
        RuleFor(x => x)
            .Must(x => !x.GainExpirationDate.HasValue || !x.GainProductionDate.HasValue
                       || x.GainExpirationDate.Value > x.GainProductionDate.Value)
            .WithMessage("过期日期必须晚于生产日期");
    }
}

/// <summary>
/// 创建并提交盘点单请求验证器（原子操作）
/// </summary>
public class CreateAndSubmitCheckDtoValidator : AbstractValidator<CreateAndSubmitCheckDto>
{
    public CreateAndSubmitCheckDtoValidator()
    {
        RuleFor(x => x.ProductId).GreaterThan(0).WithMessage("关联商品ID必须大于0");
        RuleFor(x => x.ActualQuantity).GreaterThanOrEqualTo(0).WithMessage("实际数量不能为负数");

        // 盘亏批次扣减明细校验（若提供）
        RuleForEach(x => x.DeductBatches).ChildRules(item =>
        {
            item.RuleFor(i => i.BatchId).GreaterThan(0).WithMessage("批次ID必须大于0");
            item.RuleFor(i => i.Quantity).GreaterThan(0).WithMessage("扣减数量必须大于0");
        });

        // 不允许重复批次ID
        RuleFor(x => x.DeductBatches)
            .Must(list => list == null || list.Select(i => i.BatchId).Distinct().Count() == list.Count)
            .WithMessage("扣减明细中存在重复的批次ID");

        // 盘盈批次累加明细校验（若提供）
        RuleForEach(x => x.GainBatches).ChildRules(item =>
        {
            item.RuleFor(i => i.BatchId).GreaterThan(0).WithMessage("批次ID必须大于0");
            item.RuleFor(i => i.Quantity).GreaterThan(0).WithMessage("累加数量必须大于0");
        });

        // 不允许重复批次ID
        RuleFor(x => x.GainBatches)
            .Must(list => list == null || list.Select(i => i.BatchId).Distinct().Count() == list.Count)
            .WithMessage("盘盈累加明细中存在重复的批次ID");
    }
}

using FluentValidation;
using Bms.Store.Application.Dtos.Inventories;
using Bms.Store.Domain.Entities;

namespace Bms.Store.Application.Validators;

/// <summary>
/// 出库请求 DTO 验证规则
/// </summary>
public class OutboundCreateDtoValidator : AbstractValidator<OutboundCreateDto>
{
    /// <summary>合法的出库来源类型集合（仅允许手动出库来源，盘点/调拨/采购退货等由专门流程产生）</summary>
    private static readonly HashSet<int> ValidOutboundSourceTypes = new()
    {
        InventoryLogSourceTypes.Other,                     // 10
        InventoryLogSourceTypes.SampleReceiveOutbound,     // 8
        InventoryLogSourceTypes.GiftOutbound               // 9
    };

    public OutboundCreateDtoValidator()
    {
        RuleFor(x => x.ProductId)
            .GreaterThan(0).WithMessage("商品ID无效");

        RuleFor(x => x.SourceType)
            .Must(st => ValidOutboundSourceTypes.Contains(st))
            .WithMessage("出库来源类型不合法，仅允许 8=样品领用 9=赠品活动 10=其他");

        // Quantity 与 BatchItems 互斥：只能填一个
        RuleFor(x => x)
            .Must(x => (x.Quantity.HasValue && (x.BatchItems == null || x.BatchItems.Count == 0))
                       || (!x.Quantity.HasValue && x.BatchItems != null && x.BatchItems.Count > 0))
            .WithMessage("请指定出库数量或选择批次（二者只能填一个）");

        // FEFO 模式：Quantity > 0
        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("出库数量必须大于0")
            .When(x => x.Quantity.HasValue);

        // 手动模式：BatchItems 逐项校验
        RuleForEach(x => x.BatchItems)
            .ChildRules(item =>
            {
                item.RuleFor(i => i.BatchId).GreaterThan(0).WithMessage("批次ID无效");
                item.RuleFor(i => i.Quantity).GreaterThan(0).WithMessage("批次扣减数量必须大于0");
            })
            .When(x => x.BatchItems != null && x.BatchItems.Count > 0);

        // 手动模式：无重复 BatchId
        RuleFor(x => x.BatchItems)
            .Must(items => items == null || items.Select(i => i.BatchId).Distinct().Count() == items.Count)
            .WithMessage("批次扣减明细中存在重复的批次")
            .When(x => x.BatchItems != null && x.BatchItems.Count > 0);
    }
}
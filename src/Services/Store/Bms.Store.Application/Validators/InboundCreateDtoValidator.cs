using FluentValidation;
using Bms.Store.Application.Dtos.Inventories;
using Bms.Store.Domain.Entities;

namespace Bms.Store.Application.Validators;

/// <summary>
/// 入库请求 DTO 验证规则
/// </summary>
public class InboundCreateDtoValidator : AbstractValidator<InboundCreateDto>
{
    /// <summary>合法的入库来源类型集合</summary>
    private static readonly HashSet<int> ValidInboundSourceTypes = new()
    {
        InventoryLogSourceTypes.PurchaseInbound,    // 1
        InventoryLogSourceTypes.ReturnInbound,      // 2
        InventoryLogSourceTypes.CheckAdjustment,    // 3
        InventoryLogSourceTypes.TransferInbound,    // 4
        InventoryLogSourceTypes.Other               // 6
    };

    public InboundCreateDtoValidator()
    {
        RuleFor(x => x.ProductId)
            .GreaterThan(0).WithMessage("商品ID无效");

        RuleFor(x => x.SourceType)
            .Must(st => ValidInboundSourceTypes.Contains(st))
            .WithMessage("入库来源类型不合法，仅允许 1=采购 2=退货 3=盘点 4=调拨 6=其他");

        RuleFor(x => x.SupplierId)
            .NotNull().When(x => x.SourceType == InventoryLogSourceTypes.PurchaseInbound)
            .WithMessage("采购入库必须选择供应商");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("入库数量必须大于0");

        RuleFor(x => x)
            .Must(x => !x.ProductionDate.HasValue || !x.ExpirationDate.HasValue
                       || x.ExpirationDate > x.ProductionDate)
            .WithMessage("过期日期不能早于生产日期");
    }
}

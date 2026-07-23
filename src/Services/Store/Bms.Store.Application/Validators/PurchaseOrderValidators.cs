using FluentValidation;
using Bms.Store.Application.Dtos.PurchaseOrders;
using Bms.Store.Application.Services;

namespace Bms.Store.Application.Validators;

/// <summary>
/// 创建采购订单请求验证器
/// </summary>
public class PurchaseOrderCreateDtoValidator : AbstractValidator<PurchaseOrderCreateDto>
{
    public PurchaseOrderCreateDtoValidator()
    {
        RuleFor(x => x.OrderNo)
            .NotEmpty().WithMessage("采购单号不能为空")
            .MaximumLength(50).WithMessage("采购单号最多50个字符")
            .Must(PurchaseOrderNoValidator.IsValid).WithMessage("采购单号必须为 8 位日期格式 YYYYMMDD（如 20260718）");
        RuleFor(x => x.SupplierId).GreaterThan(0).WithMessage("供应商ID必须大于0");
        RuleFor(x => x.OrderDate).NotEmpty().WithMessage("采购日期不能为空");
        RuleFor(x => x.TotalAmount).GreaterThanOrEqualTo(0).WithMessage("采购总金额必须大于等于0");
        RuleFor(x => x.Status).Must(s => s >= 1 && s <= 4).WithMessage("状态只能为1(待审核)、2(已审核)、3(已入库)或4(已取消)");
        RuleFor(x => x.PurchaseType).Must(t => t == 1 || t == 2).WithMessage("采购类型只能为1(零售商品采购)或2(耗材采购)");
    }
}

/// <summary>
/// 更新采购订单请求验证器
/// </summary>
public class PurchaseOrderUpdateDtoValidator : AbstractValidator<PurchaseOrderUpdateDto>
{
    public PurchaseOrderUpdateDtoValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("ID无效");
        Include(new PurchaseOrderCreateDtoValidator());
    }
}
